using System.Security.Claims;
using AspnetCoreMvcFull.Services.Role;

namespace AspnetCoreMvcFull.Helpers
{
  public static class AuthorizationHelper
  {
    /// <summary>
    /// Verifies if the current user has the specified role
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

      return await roleService.UserHasRoleAsync(ldapUser, roleName);
    }
  }
}
