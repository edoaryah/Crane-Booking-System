using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.MaintenanceManagement;
using AspnetCoreMvcFull.Models.Common;
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

    // GET: /MaintenanceHistory
    public async Task<IActionResult> Index(MaintenanceHistoryFilterRequest filter)
    {
      try
      {
        // Initialize filter if null
        filter ??= new MaintenanceHistoryFilterRequest();

        // Get crane list for dropdown
        filter.CraneList = await GetCraneSelectListAsync();

        // Get paged maintenance schedules
        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(filter);

        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter,
          SuccessMessage = TempData["MaintenanceHistorySuccessMessage"] as string,
          ErrorMessage = TempData["MaintenanceHistoryErrorMessage"] as string
        };

        // Clear TempData after use
        TempData.Remove("MaintenanceHistorySuccessMessage");
        TempData.Remove("MaintenanceHistoryErrorMessage");

        // For AJAX requests, return partial view
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
          return PartialView("_MaintenanceHistoryTable", viewModel);
        }

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history");

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
          return Json(new { success = false, message = ex.Message });
        }

        TempData["MaintenanceHistoryErrorMessage"] = "Error loading maintenance history: " + ex.Message;
        return View(new MaintenanceHistoryPagedViewModel
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
          ErrorMessage = ex.Message
        });
      }
    }

    // GET: /MaintenanceHistory/Details/{documentNumber}
    public async Task<IActionResult> Details(string documentNumber)
    {
      try
      {
        var schedule = await _maintenanceService.GetMaintenanceScheduleByDocumentNumberAsync(documentNumber);
        return View(schedule);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance schedule details for document number: {DocumentNumber}", documentNumber);
        TempData["MaintenanceHistoryErrorMessage"] = "Error loading maintenance details: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // GET: /MaintenanceHistory/Crane/{craneId}
    public async Task<IActionResult> Crane(int craneId)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(craneId);
        if (crane == null)
        {
          return NotFound();
        }

        // Create filter
        var filter = new MaintenanceHistoryFilterRequest
        {
          CraneId = craneId,
          PageNumber = 1,
          PageSize = 10,
          CraneList = await GetCraneSelectListAsync()
        };

        // Get schedules
        var pagedSchedules = await _maintenanceService.GetPagedMaintenanceSchedulesAsync(filter);

        var viewModel = new MaintenanceHistoryPagedViewModel
        {
          PagedSchedules = pagedSchedules,
          Filter = filter,
          SuccessMessage = TempData["MaintenanceHistorySuccessMessage"] as string,
          ErrorMessage = TempData["MaintenanceHistoryErrorMessage"] as string
        };

        // Hapus TempData setelah digunakan
        TempData.Remove("MaintenanceHistorySuccessMessage");
        TempData.Remove("MaintenanceHistoryErrorMessage");

        ViewBag.CraneName = crane.Code;
        ViewBag.CraneId = craneId;

        return View("Index", viewModel);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history for crane ID: {CraneId}", craneId);
        TempData["MaintenanceHistoryErrorMessage"] = "Error loading maintenance history: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // Helper method to get crane select list
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
