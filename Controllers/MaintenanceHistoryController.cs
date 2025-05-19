using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.MaintenanceManagement;

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
    public async Task<IActionResult> Index()
    {
      try
      {
        var schedules = await _maintenanceService.GetAllMaintenanceSchedulesAsync();

        var viewModel = new MaintenanceListViewModel
        {
          Schedules = schedules,
          SuccessMessage = TempData["MaintenanceHistorySuccessMessage"] as string,
          ErrorMessage = TempData["MaintenanceHistoryErrorMessage"] as string
        };

        // Hapus TempData setelah digunakan
        TempData.Remove("MaintenanceHistorySuccessMessage");
        TempData.Remove("MaintenanceHistoryErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance history");
        TempData["MaintenanceHistoryErrorMessage"] = "Error loading maintenance history: " + ex.Message;
        return View(new MaintenanceListViewModel { ErrorMessage = ex.Message });
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
        var schedules = await _maintenanceService.GetMaintenanceSchedulesByCraneIdAsync(craneId);

        var viewModel = new MaintenanceListViewModel
        {
          Schedules = schedules,
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
  }
}
