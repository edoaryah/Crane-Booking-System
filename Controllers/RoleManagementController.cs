using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.ViewModels.Role;
using AspnetCoreMvcFull.Models.Role;
using System.Security.Claims;
using AspnetCoreMvcFull.Filters;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize]
  [ServiceFilter(typeof(AuthorizationFilter))]
  [RequireRole("admin")]  // ✅ Tetap ada requirement, tapi bisa di-bypass oleh Super Admin yang aktif
  public class RoleManagementController : Controller
  {
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleManagementController> _logger;
    private readonly IConfiguration _configuration;

    public RoleManagementController(IRoleService roleService, ILogger<RoleManagementController> logger, IConfiguration configuration)
    {
      _roleService = roleService;
      _logger = logger;
      _configuration = configuration;
    }

    #region Role Views

    [HttpGet]
    public async Task<IActionResult> Index()
    {
      try
      {
        // ✅ SMART SUPER ADMIN CHECK
        var currentUser = User.FindFirst("ldapuser")?.Value;
        var superAdminStatus = await GetSuperAdminStatusAsync(currentUser);

        if (superAdminStatus.IsActive)
        {
          _logger.LogInformation("Super admin {User} accessing role management", currentUser);
          ViewBag.IsSuperAdmin = true;
          ViewBag.SuperAdminMessage = superAdminStatus.Message;
        }

        var roles = await _roleService.GetAllRolesAsync();
        var viewModel = new RoleIndexViewModel
        {
          Roles = roles
        };

        // Tampilkan pesan dari TempData menggunakan ViewBag
        ViewBag.SuccessMessage = TempData["RoleSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["RoleErrorMessage"] as string;

        // Hapus TempData setelah digunakan
        TempData.Remove("RoleSuccessMessage");
        TempData.Remove("RoleErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading role management index page");

        // Gunakan ViewBag untuk pesan error alih-alih model
        ViewBag.ErrorMessage = "Terjadi kesalahan saat memuat data role.";

        return View(new RoleIndexViewModel { Roles = new List<RoleSummaryViewModel>() });
      }
    }

    [HttpGet]
    public async Task<IActionResult> Users(string roleName)
    {
      try
      {
        // ✅ SMART SUPER ADMIN CHECK
        var currentUser = User.FindFirst("ldapuser")?.Value;
        var superAdminStatus = await GetSuperAdminStatusAsync(currentUser);

        if (superAdminStatus.IsActive)
        {
          _logger.LogInformation("Super admin {User} accessing users for role {RoleName}", currentUser, roleName);
          ViewBag.IsSuperAdmin = true;
          ViewBag.SuperAdminMessage = superAdminStatus.Message;
        }

        // Validate role exists
        var role = await _roleService.GetRoleByNameAsync(roleName);
        if (role == null)
        {
          TempData["RoleErrorMessage"] = $"Role {roleName} tidak ditemukan.";
          return RedirectToAction("Index");
        }

        // Get users in role
        var users = await _roleService.GetUsersByRoleNameAsync(roleName);

        var viewModel = new RoleUsersViewModel
        {
          RoleName = roleName,
          RoleDescription = role.Description,
          Users = users
        };

        // ✅ TAMBAHAN: Informasi apakah role ini bisa di-manage manual
        ViewBag.IsManagerRole = roleName.ToLower() == Roles.Manager.ToLower();
        ViewBag.CanManageUsers = Roles.AssignableRoles.Contains(roleName.ToLower());

        // Tampilkan pesan dari TempData menggunakan ViewBag
        ViewBag.SuccessMessage = TempData["RoleSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["RoleErrorMessage"] as string;

        // Hapus TempData setelah digunakan
        TempData.Remove("RoleSuccessMessage");
        TempData.Remove("RoleErrorMessage");

        // Pass role id to view
        ViewData["RoleName"] = roleName;

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading users for role: {RoleName}", roleName);
        TempData["RoleErrorMessage"] = "Terjadi kesalahan saat memuat data user.";
        return RedirectToAction("Index");
      }
    }

    #endregion

    #region AJAX Methods for Role Users

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(UserRoleCreateViewModel model)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          return Json(new { success = false, message = "Data tidak valid." });
        }

        // ✅ TAMBAHAN: Cek apakah role bisa di-assign manual
        if (model.RoleName.ToLower() == Roles.Manager.ToLower())
        {
          return Json(new { success = false, message = "Role Manager tidak dapat di-assign secara manual. Role ini otomatis berdasarkan position level di database karyawan." });
        }

        // Get current user's ldap
        var currentUser = User.FindFirst("ldapuser")?.Value ?? "system";

        // ✅ SMART SUPER ADMIN LOG
        var superAdminStatus = await GetSuperAdminStatusAsync(currentUser);
        if (superAdminStatus.IsActive)
        {
          _logger.LogInformation("Super admin {User} adding user {TargetUser} to role {RoleName}",
                               currentUser, model.LdapUser, model.RoleName);
        }

        // Validate role
        if (!await _roleService.IsRoleValidAsync(model.RoleName))
        {
          return Json(new { success = false, message = $"Role {model.RoleName} tidak valid." });
        }

        // Add user to role
        var result = await _roleService.AssignRoleToUserAsync(model, currentUser);

        // ✅ SPECIAL HANDLING: Jika assign role Admin, beri notifikasi tentang SuperAdmin
        var responseMessage = $"User {result.EmployeeName} berhasil ditambahkan ke role {model.RoleName}.";

        if (model.RoleName.ToLower() == "admin" && superAdminStatus.IsActive)
        {
          responseMessage += " \n\nℹ️ Super Admin akan otomatis nonaktif karena sudah ada user dengan role Admin.";
          _logger.LogInformation("SuperAdmin will be auto-disabled due to Admin role assignment");
        }

        return Json(new
        {
          success = true,
          message = responseMessage,
          user = result,
          superAdminDisabled = model.RoleName.ToLower() == "admin" && superAdminStatus.IsActive
        });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogWarning(ex, "Invalid operation when adding user to role");
        return Json(new { success = false, message = ex.Message });
      }
      catch (KeyNotFoundException ex)
      {
        _logger.LogWarning(ex, "Entity not found when adding user to role");
        return Json(new { success = false, message = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error adding user to role");
        return Json(new { success = false, message = "Terjadi kesalahan saat menambahkan user ke role." });
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(UserRoleUpdateViewModel model)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          return Json(new { success = false, message = "Data tidak valid." });
        }

        // Get current user's ldap
        var currentUser = User.FindFirst("ldapuser")?.Value ?? "system";

        // ✅ SMART SUPER ADMIN LOG
        var superAdminStatus = await GetSuperAdminStatusAsync(currentUser);
        if (superAdminStatus.IsActive)
        {
          _logger.LogInformation("Super admin {User} updating user role ID {RoleId}", currentUser, model.Id);
        }

        // Update user role
        var updatedUserRole = await _roleService.UpdateUserRoleAsync(model.Id, model, currentUser);

        return Json(new
        {
          success = true,
          message = $"User role berhasil diupdate.",
          userRole = updatedUserRole
        });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogWarning(ex, "Invalid operation when updating user role");
        return Json(new { success = false, message = ex.Message });
      }
      catch (KeyNotFoundException ex)
      {
        _logger.LogWarning(ex, "Entity not found when updating user role");
        return Json(new { success = false, message = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating user role");
        return Json(new { success = false, message = "Terjadi kesalahan saat mengupdate user role." });
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(int id)
    {
      try
      {
        // Get current user's ldap
        var currentUser = User.FindFirst("ldapuser")?.Value ?? "system";

        // ✅ SMART SUPER ADMIN LOG
        var superAdminStatus = await GetSuperAdminStatusAsync(currentUser);
        if (superAdminStatus.IsActive)
        {
          _logger.LogInformation("Super admin {User} removing user role ID {RoleId}", currentUser, id);
        }

        await _roleService.RemoveRoleFromUserAsync(id);

        return Json(new
        {
          success = true,
          message = "User berhasil dihapus dari role."
        });
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogWarning(ex, "Invalid operation when removing user from role");
        return Json(new { success = false, message = ex.Message });
      }
      catch (KeyNotFoundException ex)
      {
        _logger.LogWarning(ex, "Entity not found when removing user from role");
        return Json(new { success = false, message = ex.Message });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error removing user from role");
        return Json(new { success = false, message = "Terjadi kesalahan saat menghapus user dari role." });
      }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableEmployees(string roleName, string? department = null)
    {
      try
      {
        // Get current user's ldap
        var currentUser = User.FindFirst("ldapuser")?.Value ?? "system";

        // ✅ TAMBAHAN: Cek apakah role bisa di-assign manual
        if (roleName.ToLower() == Roles.Manager.ToLower())
        {
          return Json(new { success = false, message = "Role Manager tidak dapat di-assign secara manual." });
        }

        // Validate role
        if (!await _roleService.IsRoleValidAsync(roleName))
        {
          return Json(new { success = false, message = $"Role {roleName} tidak valid." });
        }

        var employees = await _roleService.GetEmployeesNotInRoleAsync(roleName, department);

        // Decode HTML entities pada semua kolom teks
        foreach (var employee in employees)
        {
          if (employee.Name != null)
            employee.Name = System.Web.HttpUtility.HtmlDecode(employee.Name);

          if (employee.Department != null)
            employee.Department = System.Web.HttpUtility.HtmlDecode(employee.Department);

          if (employee.Position != null)
            employee.Position = System.Web.HttpUtility.HtmlDecode(employee.Position);
        }

        return Json(new
        {
          success = true,
          employees
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting available employees for role {RoleName}", roleName);
        return Json(new { success = false, message = "Terjadi kesalahan saat mengambil data karyawan." });
      }
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
      try
      {
        // Menggunakan service untuk mengambil data departemen
        var departments = await _roleService.GetAllDepartmentsAsync();

        // Decode HTML entities sebelum dikirim ke client
        var decodedDepartments = departments.Select(dept =>
            System.Web.HttpUtility.HtmlDecode(dept)).ToList();

        return Json(new
        {
          success = true,
          departments = decodedDepartments
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error mengambil data departemen");
        return Json(new { success = false, message = "Terjadi kesalahan saat mengambil data departemen." });
      }
    }

    #endregion

    #region ✅ SMART SUPER ADMIN Helper Methods

    /// <summary>
    /// ✅ SMART CHECK: Mendapatkan status SuperAdmin yang cerdas
    /// </summary>
    private async Task<SuperAdminStatus> GetSuperAdminStatusAsync(string? ldapUser)
    {
      if (string.IsNullOrEmpty(ldapUser))
      {
        return new SuperAdminStatus { IsActive = false, Message = "No LDAP user" };
      }

      try
      {
        // 1. Cek apakah user ada dalam daftar SuperAdmins di config
        if (!IsSuperAdminInConfig(ldapUser))
        {
          return new SuperAdminStatus { IsActive = false, Message = "Not in SuperAdmins config" };
        }

        // 2. SMART CHECK: Apakah sudah ada user dengan role Admin?
        var hasAdminUsers = await _roleService.GetUsersByRoleNameAsync("admin");

        if (hasAdminUsers != null && hasAdminUsers.Any())
        {
          return new SuperAdminStatus
          {
            IsActive = false,
            Message = $"SuperAdmin auto-disabled: {hasAdminUsers.Count} Admin user(s) already exist"
          };
        }

        // 3. Jika belum ada Admin, SuperAdmin tetap aktif
        return new SuperAdminStatus
        {
          IsActive = true,
          Message = $"You are accessing as Super Admin ({ldapUser}). This bypass is active until an Admin role is assigned."
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking smart super admin status for user {LdapUser}", ldapUser);
        return new SuperAdminStatus { IsActive = false, Message = "Error checking status" };
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

    /// <summary>
    /// ✅ Helper class untuk status SuperAdmin
    /// </summary>
    private class SuperAdminStatus
    {
      public bool IsActive { get; set; }
      public string Message { get; set; } = string.Empty;
    }

    #endregion
  }
}
