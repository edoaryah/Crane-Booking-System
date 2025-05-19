using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.ViewModels.Role;
using System.Security.Claims;
using AspnetCoreMvcFull.Filters;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize]
  [ServiceFilter(typeof(AuthorizationFilter))]
  // [RequireRole("admin")]
  // [RequireRole("pic")]
  public class RoleManagementController : Controller
  {
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleManagementController> _logger;

    public RoleManagementController(IRoleService roleService, ILogger<RoleManagementController> logger)
    {
      _roleService = roleService;
      _logger = logger;
    }

    #region Role Views

    [HttpGet]
    public async Task<IActionResult> Index()
    {
      try
      {
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

        // Get current user's ldap
        var currentUser = User.FindFirst("ldapuser")?.Value ?? "system";

        // Validate role
        if (!await _roleService.IsRoleValidAsync(model.RoleName))
        {
          return Json(new { success = false, message = $"Role {model.RoleName} tidak valid." });
        }

        // Add user to role
        var result = await _roleService.AssignRoleToUserAsync(model, currentUser);

        return Json(new
        {
          success = true,
          message = $"User {result.EmployeeName} berhasil ditambahkan ke role {model.RoleName}.",
          user = result
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

        // Update user role
        var updatedUserRole = await _roleService.UpdateUserRoleAsync(model.Id, model, currentUser);

        return Json(new
        {
          success = true,
          message = $"User role berhasil diupdate.",
          userRole = updatedUserRole
        });
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

        await _roleService.RemoveRoleFromUserAsync(id);

        return Json(new
        {
          success = true,
          message = "User berhasil dihapus dari role."
        });
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
  }
}
