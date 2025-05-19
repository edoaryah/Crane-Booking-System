using System.ComponentModel.DataAnnotations;
using AspnetCoreMvcFull.Models.Auth;

namespace AspnetCoreMvcFull.ViewModels.Role
{
  public class UserRoleViewModel
  {
    public int Id { get; set; }
    public string LdapUser { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Employee details
    public string? EmployeeName { get; set; }
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
  }

  public class RoleIndexViewModel
  {
    public List<RoleSummaryViewModel> Roles { get; set; } = new List<RoleSummaryViewModel>();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
  }

  public class RoleSummaryViewModel
  {
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
  }

  public class RoleUsersViewModel
  {
    public string RoleName { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }
    public List<UserRoleViewModel> Users { get; set; } = new List<UserRoleViewModel>();
  }

  public class UserRoleCreateViewModel
  {
    [Required(ErrorMessage = "User harus dipilih")]
    public string LdapUser { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role harus dipilih")]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Catatan tidak boleh lebih dari 255 karakter")]
    public string? Notes { get; set; }
  }

  public class UserRoleUpdateViewModel
  {
    public int Id { get; set; }

    [StringLength(255, ErrorMessage = "Catatan tidak boleh lebih dari 255 karakter")]
    public string? Notes { get; set; }
  }

  public class AvailableEmployeeViewModel
  {
    public string LdapUser { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EmpId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
  }

  public class EmployeeFilterViewModel
  {
    public string? Department { get; set; }
  }
}
