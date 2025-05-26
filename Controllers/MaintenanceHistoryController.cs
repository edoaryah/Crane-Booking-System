using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspnetCoreMvcFull.Filters;  // ← USE EXISTING FILTER
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.ViewModels.MaintenanceManagement;
using AspnetCoreMvcFull.Models.Common;
using AspnetCoreMvcFull.Helpers;  // ← ADD THIS FOR AuthorizationHelper
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]  // ← KEEP THIS
  [RequireRole("admin")]  // ← ADD THESE TO ENTIRE CONTROLLER
  [RequireRole("pic")]
  [RequireRole("msd")]
  public class MaintenanceHistoryController : Controller
  {
    private readonly IMaintenanceScheduleService _maintenanceService;
    private readonly ICraneService _craneService;
    private readonly IRoleService _roleService;
    private readonly ILogger<MaintenanceHistoryController> _logger;

    public MaintenanceHistoryController(
        IMaintenanceScheduleService maintenanceService,
        ICraneService craneService,
        IRoleService roleService,
        ILogger<MaintenanceHistoryController> logger)
    {
      _maintenanceService = maintenanceService;
      _craneService = craneService;
      _roleService = roleService;
      _logger = logger;
    }

    /// <summary>
    /// Main entry point for the maintenance history page.
    /// </summary>
    public async Task<IActionResult> Index(MaintenanceHistoryFilterRequest filter)
    {
      try
      {
        filter ??= new MaintenanceHistoryFilterRequest();

        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "CreatedAt";

        // ✅ SIMPLIFIED - no manual permission check needed (handled by attributes)
        var currentUser = User.FindFirst("ldapuser")?.Value;
        var userRoles = new List<string>();

        if (!string.IsNullOrEmpty(currentUser))
        {
          try
          {
            userRoles = await _roleService.GetUserRolesAsync(currentUser);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to get user roles for {CurrentUser}", currentUser);
            userRoles = new List<string>();
          }
        }

        filter.CraneList = await GetCraneSelectListAsync();

        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(
            filter, currentUser, userRoles);

        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter,
          SuccessMessage = TempData["SuccessMessage"] as string,
          ErrorMessage = TempData["ErrorMessage"] as string
        };

        // ✅ USE AUTHORIZATION HELPER for role checking
        ViewBag.CurrentUser = userName;
        ViewBag.CurrentLdapUser = currentUser;
        ViewBag.UserRoles = userRoles;
        ViewBag.IsAdmin = await AuthorizationHelper.HasRole(User, _roleService, "admin");
        ViewBag.IsPic = await AuthorizationHelper.HasRole(User, _roleService, "pic");
        ViewBag.IsMsd = await AuthorizationHelper.HasRole(User, _roleService, "msd");

        _logger.LogInformation("User {LdapUser} with roles [{Roles}] accessed maintenance history",
                             currentUser, string.Join(", ", userRoles));

        TempData.Remove("SuccessMessage");
        TempData.Remove("ErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history");

        var errorModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = new PagedResult<MaintenanceScheduleViewModel>
          {
            Items = Enumerable.Empty<MaintenanceScheduleViewModel>(),
            TotalCount = 0,
            PageCount = 0,
            PageNumber = 1,
            PageSize = 10
          },
          Filter = new MaintenanceHistoryFilterRequest
          {
            CraneList = await GetCraneSelectListAsync()
          },
          ErrorMessage = "Terjadi kesalahan saat memuat data: " + ex.Message
        };

        return View(errorModel);
      }
    }

    /// <summary>
    /// AJAX endpoint to get just the table portion.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTableData(MaintenanceHistoryFilterRequest filter)
    {
      try
      {
        filter ??= new MaintenanceHistoryFilterRequest();

        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "CreatedAt";

        // ✅ SIMPLIFIED - no manual permission check needed (handled by attributes)
        var currentUser = User.FindFirst("ldapuser")?.Value;
        var userRoles = new List<string>();

        if (!string.IsNullOrEmpty(currentUser))
        {
          try
          {
            userRoles = await _roleService.GetUserRolesAsync(currentUser);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to get user roles for {CurrentUser} in AJAX request", currentUser);
            userRoles = new List<string>();
          }
        }

        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(
            filter, currentUser, userRoles);

        filter.CraneList = await GetCraneSelectListAsync();

        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter
        };

        return PartialView("_MaintenanceHistoryTable", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history table data");

        return Content("<div class='alert alert-danger m-3'>" +
                      "<i class='bx bx-error-circle me-2'></i>" +
                      "Terjadi kesalahan saat memuat data: " + ex.Message +
                      "</div>", "text/html");
      }
    }

    /// <summary>
    /// Filter maintenance for a specific crane.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Crane(int craneId)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(craneId);
        if (crane == null)
        {
          return NotFound("Crane not found");
        }

        var currentUser = User.FindFirst("ldapuser")?.Value;
        var userRoles = new List<string>();

        if (!string.IsNullOrEmpty(currentUser))
        {
          userRoles = await _roleService.GetUserRolesAsync(currentUser);
        }

        var filter = new MaintenanceHistoryFilterRequest
        {
          CraneId = craneId,
          PageNumber = 1,
          PageSize = 10,
          SortBy = "CreatedAt",
          SortDesc = true,
          CraneList = await GetCraneSelectListAsync()
        };

        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(
            filter, currentUser, userRoles);

        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter,
          SuccessMessage = TempData["SuccessMessage"] as string,
          ErrorMessage = TempData["ErrorMessage"] as string
        };

        TempData.Remove("SuccessMessage");
        TempData.Remove("ErrorMessage");

        ViewBag.CraneName = crane.Code;
        ViewBag.CraneId = craneId;

        // ✅ USE AUTHORIZATION HELPER for role checking
        ViewBag.CurrentUser = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
        ViewBag.UserRoles = userRoles;
        ViewBag.IsAdmin = await AuthorizationHelper.HasRole(User, _roleService, "admin");
        ViewBag.IsPic = await AuthorizationHelper.HasRole(User, _roleService, "pic");
        ViewBag.IsMsd = await AuthorizationHelper.HasRole(User, _roleService, "msd");

        return View("Index", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history for crane ID: {CraneId}", craneId);
        TempData["ErrorMessage"] = "Error loading maintenance history: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    /// <summary>
    /// Helper method to get crane select list for dropdowns.
    /// </summary>
    private async Task<List<SelectListItem>> GetCraneSelectListAsync()
    {
      var cranes = await _craneService.GetAllCranesAsync();
      return cranes.Select(c => new SelectListItem
      {
        Value = c.Id.ToString(),
        Text = $"{c.Code} - {c.Capacity} ton"
      }).ToList();
    }
  }
}
