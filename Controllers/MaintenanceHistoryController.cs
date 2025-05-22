using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.MaintenanceManagement;
using AspnetCoreMvcFull.Models.Common;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class MaintenanceHistoryController : Controller
  {
    private readonly IMaintenanceScheduleService _maintenanceService;
    private readonly ICraneService _craneService;
    private readonly ILogger<MaintenanceHistoryController> _logger;

    public MaintenanceHistoryController(
        IMaintenanceScheduleService maintenanceService,
        ICraneService craneService,
        ILogger<MaintenanceHistoryController> logger)
    {
      _maintenanceService = maintenanceService;
      _craneService = craneService;
      _logger = logger;
    }

    /// <summary>
    /// Main entry point for the maintenance history page.
    /// Displays the full page with filters and data table.
    /// </summary>
    public async Task<IActionResult> Index(MaintenanceHistoryFilterRequest filter)
    {
      try
      {
        // Initialize filter if null and ensure valid defaults
        filter ??= new MaintenanceHistoryFilterRequest();

        // Validate pagination parameters
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "CreatedAt";

        // Get crane list for dropdown
        filter.CraneList = await GetCraneSelectListAsync();

        // Get paged data
        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(filter);

        // Get current user from claims (sama seperti pattern Anda)
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        // Build view model
        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter,
          SuccessMessage = TempData["SuccessMessage"] as string,
          ErrorMessage = TempData["ErrorMessage"] as string
        };

        // Pass user info to view
        ViewBag.CurrentUser = userName;

        // Clear TempData after use
        TempData.Remove("SuccessMessage");
        TempData.Remove("ErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history");

        // Create empty model with error message
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
    /// AJAX endpoint to get just the table portion of the page.
    /// Used for filtering, pagination, etc.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTableData(MaintenanceHistoryFilterRequest filter)
    {
      try
      {
        // Initialize filter if null and ensure valid defaults
        filter ??= new MaintenanceHistoryFilterRequest();

        // Validate pagination parameters
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "CreatedAt";

        // Get paged data
        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(filter);

        // Get crane list for dropdown (needed for when returning to main view)
        filter.CraneList = await GetCraneSelectListAsync();

        // Build view model for partial
        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter
        };

        // Return partial view
        return PartialView("_MaintenanceHistoryTable", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history table data");

        // Return error message that will be displayed in the table area
        return Content("<div class='alert alert-danger m-3'>" +
                      "<i class='bx bx-error-circle me-2'></i>" +
                      "Terjadi kesalahan saat memuat data: " + ex.Message +
                      "</div>", "text/html");
      }
    }

    /// <summary>
    /// Display details for a specific maintenance schedule.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string documentNumber)
    {
      try
      {
        if (string.IsNullOrEmpty(documentNumber))
        {
          return BadRequest("Document number is required");
        }

        var schedule = await _maintenanceService.GetMaintenanceScheduleByDocumentNumberAsync(documentNumber);
        return View(schedule);
      }
      catch (KeyNotFoundException)
      {
        return NotFound("Maintenance schedule not found");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance schedule details for document number: {DocumentNumber}", documentNumber);
        TempData["ErrorMessage"] = "Error loading maintenance details: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    /// <summary>
    /// Filter maintenance history for a specific crane.
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

        // Create filter specifically for this crane
        var filter = new MaintenanceHistoryFilterRequest
        {
          CraneId = craneId,
          PageNumber = 1,
          PageSize = 10,
          SortBy = "CreatedAt",
          SortDesc = true,
          CraneList = await GetCraneSelectListAsync()
        };

        // Get schedules
        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(filter);

        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter,
          SuccessMessage = TempData["SuccessMessage"] as string,
          ErrorMessage = TempData["ErrorMessage"] as string
        };

        // Hapus TempData setelah digunakan
        TempData.Remove("SuccessMessage");
        TempData.Remove("ErrorMessage");

        // Add crane info to ViewBag for display
        ViewBag.CraneName = crane.Code;
        ViewBag.CraneId = craneId;

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
