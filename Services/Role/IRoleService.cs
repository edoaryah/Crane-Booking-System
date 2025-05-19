using AspnetCoreMvcFull.Models.Auth;
using AspnetCoreMvcFull.ViewModels.Role;

namespace AspnetCoreMvcFull.Services.Role
{
  public interface IRoleService
  {
    // Role-related operations
    Task<List<RoleSummaryViewModel>> GetAllRolesAsync();
    Task<RoleSummaryViewModel?> GetRoleByNameAsync(string name);

    // User-role operations
    Task<List<UserRoleViewModel>> GetUsersByRoleNameAsync(string roleName);
    Task<UserRoleViewModel?> GetUserRoleByIdAsync(int id);
    Task<UserRoleViewModel?> GetUserRoleByLdapUserAndRoleNameAsync(string ldapUser, string roleName);
    Task<UserRoleViewModel> AssignRoleToUserAsync(UserRoleCreateViewModel userRoleDto, string createdBy);
    Task<UserRoleViewModel> UpdateUserRoleAsync(int id, UserRoleUpdateViewModel userRoleDto, string updatedBy);
    Task RemoveRoleFromUserAsync(int userRoleId);
    Task<bool> UserHasRoleAsync(string ldapUser, string roleName);
    Task<List<AvailableEmployeeViewModel>> GetEmployeesNotInRoleAsync(string roleName, string? department = null);

    // Tambahkan method ini ke interface IRoleService.cs
    Task<List<string>> GetAllDepartmentsAsync();

    // Helper methods
    Task<bool> IsRoleValidAsync(string roleName);
  }
}
