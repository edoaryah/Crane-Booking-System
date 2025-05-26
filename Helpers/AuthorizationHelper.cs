using System.Security.Claims;
using AspnetCoreMvcFull.Services.Role;

namespace AspnetCoreMvcFull.Helpers
{
  public static class AuthorizationHelper
  {
    /// <summary>
    /// Verifies if the current user has the specified role
    /// ✅ UPDATED: Sekarang mendukung auto-detection untuk role manager
    /// </summary>
    public static async Task<bool> HasRole(ClaimsPrincipal user, IRoleService roleService, string roleName)
    {
      if (!user.Identity?.IsAuthenticated ?? true)
      {
        return false;
      }

      string? ldapUser = user.FindFirst("ldapuser")?.Value;
      if (string.IsNullOrEmpty(ldapUser))
      {
        return false;
      }

      // ✅ Gunakan method baru yang support auto-detection manager
      return await roleService.UserHasRoleAsync(ldapUser, roleName);
    }

    /// <summary>
    /// ✅ TAMBAHAN: Method untuk mendapatkan semua role user (termasuk auto-detected manager)
    /// </summary>
    public static async Task<List<string>> GetUserRoles(ClaimsPrincipal user, IRoleService roleService)
    {
      if (!user.Identity?.IsAuthenticated ?? true)
      {
        return new List<string>();
      }

      string? ldapUser = user.FindFirst("ldapuser")?.Value;
      if (string.IsNullOrEmpty(ldapUser))
      {
        return new List<string>();
      }

      return await roleService.GetUserRolesAsync(ldapUser);
    }

    /// <summary>
    /// ✅ TAMBAHAN: Method khusus untuk cek apakah user adalah manager
    /// </summary>
    public static async Task<bool> IsManager(ClaimsPrincipal user, IRoleService roleService)
    {
      return await HasRole(user, roleService, "manager");
    }
  }
}
