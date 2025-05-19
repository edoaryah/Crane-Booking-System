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

    public AuthorizationFilter(ILogger<AuthorizationFilter> logger, IRoleService roleService)
    {
      _logger = logger;
      _roleService = roleService;
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

      // Check if controller or action has RequireRole attributes
      var requiredRoleAttributes = context.ActionDescriptor.EndpointMetadata
          .OfType<RequireRoleAttribute>()
          .ToList();

      if (requiredRoleAttributes.Any())
      {
        // Get user's LDAP username
        var ldapUser = context.HttpContext.User.FindFirst("ldapuser")?.Value;
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
  }
}
