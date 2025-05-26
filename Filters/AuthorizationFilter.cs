using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AspnetCoreMvcFull.Services.Role;

namespace AspnetCoreMvcFull.Filters
{
  // Attribute to mark controllers or actions that require specific role
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
  public class RequireRoleAttribute : Attribute
  {
    public string RoleName { get; }

    public RequireRoleAttribute(string roleName)
    {
      RoleName = roleName;
    }
  }

  public class AuthorizationFilter : IAuthorizationFilter
  {
    private readonly ILogger<AuthorizationFilter> _logger;
    private readonly IRoleService _roleService;
    private readonly IConfiguration _configuration;

    public AuthorizationFilter(ILogger<AuthorizationFilter> logger, IRoleService roleService, IConfiguration configuration)
    {
      _logger = logger;
      _roleService = roleService;
      _configuration = configuration;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
      // Skip authorization if AllowAnonymous is applied
      if (context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType().Name == "AllowAnonymousAttribute"))
        return;

      // Check if user is authenticated
      if (context.HttpContext.User.Identity?.IsAuthenticated != true)
      {
        _logger.LogWarning("Unauthorized access attempt to {Path}", context.HttpContext.Request.Path);

        // Redirect to login page with return URL
        var returnUrl = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(context.HttpContext.Request.QueryString.Value))
        {
          returnUrl += context.HttpContext.Request.QueryString.Value;
        }

        context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
        return;
      }

      // ✅ SMART SUPER ADMIN: Auto-disable setelah ada Admin role (SYNCHRONOUS)
      var ldapUser = context.HttpContext.User.FindFirst("ldapuser")?.Value;
      if (!string.IsNullOrEmpty(ldapUser) && IsActiveSuperAdmin(ldapUser))
      {
        _logger.LogInformation("Super admin {LdapUser} bypassing role authorization for {Path}",
                             ldapUser, context.HttpContext.Request.Path);
        return; // Bypass semua cek role untuk super admin yang masih aktif
      }

      // Check if controller or action has RequireRole attributes
      var requiredRoleAttributes = context.ActionDescriptor.EndpointMetadata
          .OfType<RequireRoleAttribute>()
          .ToList();

      if (requiredRoleAttributes.Any())
      {
        // Get user's LDAP username
        if (string.IsNullOrEmpty(ldapUser))
        {
          _logger.LogWarning("User without LDAP identifier attempted to access protected resource: {Path}",
                           context.HttpContext.Request.Path);
          context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
          return;
        }

        // Check if user has any of the required roles
        var hasRequiredRole = false;

        foreach (var roleAttr in requiredRoleAttributes)
        {
          // Use .Result since we can't use await in synchronous method
          bool hasRole = _roleService.UserHasRoleAsync(ldapUser, roleAttr.RoleName).Result;

          if (hasRole)
          {
            hasRequiredRole = true;
            break;
          }
        }

        if (!hasRequiredRole)
        {
          _logger.LogWarning("Access denied to {Path} for user {LdapUser} - missing required role",
                           context.HttpContext.Request.Path, ldapUser);
          context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
          return;
        }
      }
    }

    /// <summary>
    /// ✅ SMART LOGIC: Cek apakah SuperAdmin masih aktif berdasarkan kondisi sistem (SYNCHRONOUS)
    /// </summary>
    private bool IsActiveSuperAdmin(string ldapUser)
    {
      try
      {
        // 1. Cek apakah user ada dalam daftar SuperAdmins di config
        if (!IsSuperAdminInConfig(ldapUser))
        {
          return false; // User tidak ada di config SuperAdmins
        }

        // 2. SMART CHECK: Apakah sudah ada user dengan role Admin? (SYNCHRONOUS)
        var hasAdminUsers = _roleService.GetUsersByRoleNameAsync("admin").Result;

        if (hasAdminUsers != null && hasAdminUsers.Any())
        {
          _logger.LogInformation("SuperAdmin auto-disabled: Admin role already assigned to {AdminCount} users",
                               hasAdminUsers.Count);
          return false; // Auto-disable SuperAdmin karena sudah ada Admin
        }

        // 3. Jika belum ada Admin, SuperAdmin tetap aktif
        _logger.LogInformation("SuperAdmin {LdapUser} still active - no Admin users found", ldapUser);
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking active super admin status for user {LdapUser}", ldapUser);
        return false; // Fail safe - disable SuperAdmin jika error
      }
    }

    /// <summary>
    /// Cek apakah user ada dalam daftar SuperAdmins di konfigurasi
    /// </summary>
    private bool IsSuperAdminInConfig(string ldapUser)
    {
      try
      {
        // Ambil daftar super admin dari konfigurasi
        var superAdmins = _configuration.GetSection("Security:SuperAdmins").Get<string[]>() ?? new string[0];

        // Cek apakah user ada dalam daftar super admin (case insensitive)
        return superAdmins.Any(admin =>
            string.Equals(admin.Trim(), ldapUser.Trim(), StringComparison.OrdinalIgnoreCase));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking super admin config for user {LdapUser}", ldapUser);
        return false;
      }
    }
  }
}
