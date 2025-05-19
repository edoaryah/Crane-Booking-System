using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.MaintenanceManagement;
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class MaintenanceController : Controller
  {
    private readonly ICraneService _craneService;
    private readonly IShiftDefinitionService _shiftService;
    private readonly IMaintenanceScheduleService _maintenanceService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        ICraneService craneService,
        IShiftDefinitionService shiftService,
        IMaintenanceScheduleService maintenanceService,
        ILogger<MaintenanceController> logger)
    {
      _craneService = craneService;
      _shiftService = shiftService;
      _maintenanceService = maintenanceService;
      _logger = logger;
    }

    // GET: /Maintenance
    public async Task<IActionResult> Index()
    {
      try
      {
        var viewModel = new MaintenanceFormViewModel
        {
          AvailableCranes = await _craneService.GetAllCranesAsync(),
          ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync()
        };

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance form");
        TempData["MaintenanceErrorMessage"] = "Error loading maintenance form: " + ex.Message;
        return View("Error");
      }
    }

    // GET: /Maintenance/List
    public async Task<IActionResult> List()
    {
      try
      {
        var schedules = await _maintenanceService.GetAllMaintenanceSchedulesAsync();

        var viewModel = new MaintenanceListViewModel
        {
          Schedules = schedules,
          SuccessMessage = TempData["MaintenanceSuccessMessage"] as string,
          ErrorMessage = TempData["MaintenanceErrorMessage"] as string
        };

        // Hapus TempData setelah digunakan
        TempData.Remove("MaintenanceSuccessMessage");
        TempData.Remove("MaintenanceErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance schedules");
        TempData["MaintenanceErrorMessage"] = "Error loading maintenance schedules: " + ex.Message;
        return View(new MaintenanceListViewModel { ErrorMessage = ex.Message });
      }
    }

    // GET: /Maintenance/Details/{documentNumber}
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
        TempData["MaintenanceErrorMessage"] = "Error loading maintenance details: " + ex.Message;
        return RedirectToAction(nameof(List));
      }
    }

    // POST: /Maintenance/SaveSchedule - Mengganti nama action untuk memproses form
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceScheduleCreateViewModel viewModel)
    {
      try
      {
        if (ModelState.IsValid)
        {
          // Set created by to current user
          var userName = User.FindFirst(ClaimTypes.Name)?.Value;
          var ldapUser = User.FindFirst("ldapuser")?.Value;

          viewModel.CreatedBy = userName ?? ldapUser ?? "system";

          var createdSchedule = await _maintenanceService.CreateMaintenanceScheduleAsync(viewModel);

          TempData["MaintenanceSuccessMessage"] = "Maintenance schedule created successfully";
          return RedirectToAction(nameof(Details), new { documentNumber = createdSchedule.DocumentNumber });
        }

        // Jika validasi gagal, kembali ke form dengan data yang dibutuhkan
        var formViewModel = new MaintenanceFormViewModel
        {
          AvailableCranes = await _craneService.GetAllCranesAsync(),
          ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync()
        };

        // Tambahkan pesan error
        ModelState.AddModelError("", "Silakan perbaiki error dan coba lagi.");

        return View("Index", formViewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating maintenance schedule");

        // Redirect ke Index dengan pesan error
        TempData["MaintenanceErrorMessage"] = "Error membuat jadwal maintenance: " + ex.Message;
        return RedirectToAction("Index");
      }
    }

    // GET: /Maintenance/Edit/{documentNumber}
    public async Task<IActionResult> Edit(string documentNumber)
    {
      try
      {
        var schedule = await _maintenanceService.GetMaintenanceScheduleByDocumentNumberAsync(documentNumber);

        // Convert to update view model
        var viewModel = new MaintenanceScheduleUpdateViewModel
        {
          CraneId = schedule.CraneId,
          Title = schedule.Title,
          StartDate = schedule.StartDate,
          EndDate = schedule.EndDate,
          Description = schedule.Description,
          ShiftSelections = ConvertShiftsToSelections(schedule)
        };

        ViewBag.Cranes = await _craneService.GetAllCranesAsync();
        ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
        ViewBag.DocumentNumber = schedule.DocumentNumber;
        ViewBag.ScheduleId = schedule.Id;

        return View(viewModel);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance schedule for edit with document number: {DocumentNumber}", documentNumber);
        TempData["MaintenanceErrorMessage"] = "Error loading maintenance schedule: " + ex.Message;
        return RedirectToAction(nameof(List));
      }
    }

    // POST: /Maintenance/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MaintenanceScheduleUpdateViewModel viewModel)
    {
      try
      {
        if (ModelState.IsValid)
        {
          var updatedSchedule = await _maintenanceService.UpdateMaintenanceScheduleAsync(id, viewModel);

          TempData["MaintenanceSuccessMessage"] = "Maintenance schedule updated successfully";
          return RedirectToAction(nameof(Details), new { documentNumber = updatedSchedule.DocumentNumber });
        }

        // If we got this far, something failed, redisplay form
        ViewBag.Cranes = await _craneService.GetAllCranesAsync();
        ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();

        // Try to get the document number
        var schedule = await _maintenanceService.GetMaintenanceScheduleByIdAsync(id);
        ViewBag.DocumentNumber = schedule.DocumentNumber;
        ViewBag.ScheduleId = id;

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating maintenance schedule with ID: {Id}", id);
        ModelState.AddModelError("", "Error updating maintenance schedule: " + ex.Message);

        ViewBag.Cranes = await _craneService.GetAllCranesAsync();
        ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();

        // Try to get the document number
        try
        {
          var schedule = await _maintenanceService.GetMaintenanceScheduleByIdAsync(id);
          ViewBag.DocumentNumber = schedule.DocumentNumber;
        }
        catch
        {
          // Ignore if we can't get the document number
        }

        ViewBag.ScheduleId = id;
        return View(viewModel);
      }
    }

    // GET: /Maintenance/Delete/{documentNumber}
    public async Task<IActionResult> Delete(string documentNumber)
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
        _logger.LogError(ex, "Error loading maintenance schedule for deletion with document number: {DocumentNumber}", documentNumber);
        TempData["MaintenanceErrorMessage"] = "Error loading maintenance schedule: " + ex.Message;
        return RedirectToAction(nameof(List));
      }
    }

    // POST: /Maintenance/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      try
      {
        await _maintenanceService.DeleteMaintenanceScheduleAsync(id);

        TempData["MaintenanceSuccessMessage"] = "Maintenance schedule deleted successfully";
        return RedirectToAction(nameof(List));
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting maintenance schedule with ID: {Id}", id);
        TempData["MaintenanceErrorMessage"] = "Error deleting maintenance schedule: " + ex.Message;
        return RedirectToAction(nameof(List));
      }
    }

    // GET: /Maintenance/Calendar
    public async Task<IActionResult> Calendar(DateTime? start = null, DateTime? end = null)
    {
      try
      {
        // Default to current week if not specified
        var startDate = start ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        var endDate = end ?? startDate.AddDays(6);

        // Get all cranes
        var cranes = await _craneService.GetAllCranesAsync();

        // Get all shift definitions
        var shifts = await _shiftService.GetAllShiftDefinitionsAsync();

        // Get all maintenance schedules in the date range
        var schedules = await _maintenanceService.GetAllMaintenanceSchedulesAsync();
        var filteredSchedules = schedules.Where(s =>
            (s.StartDate <= endDate && s.EndDate >= startDate)).ToList();

        var viewModel = new MaintenanceCalendarViewModel
        {
          StartDate = startDate,
          EndDate = endDate,
          Cranes = cranes.ToList(),
          ShiftDefinitions = shifts.ToList(),
          Schedules = filteredSchedules
        };

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading maintenance calendar");
        TempData["MaintenanceErrorMessage"] = "Error loading maintenance calendar: " + ex.Message;
        return View("Error");
      }
    }

    // API for checking shift conflicts
    [HttpGet]
    public async Task<IActionResult> CheckShiftConflict(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null)
    {
      try
      {
        var hasConflict = await _maintenanceService.IsShiftMaintenanceConflictAsync(
            craneId, date, shiftDefinitionId, excludeMaintenanceId);

        return Ok(new { hasConflict });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking maintenance shift conflict");
        return StatusCode(500, new { error = ex.Message });
      }
    }

    // Helper methods
    private List<DailyShiftSelectionViewModel> ConvertShiftsToSelections(MaintenanceScheduleDetailViewModel schedule)
    {
      // Group shifts by date
      var groupedShifts = schedule.Shifts.GroupBy(s => s.Date.Date)
                                       .Select(g => new
                                       {
                                         Date = g.Key,
                                         ShiftIds = g.Select(s => s.ShiftDefinitionId).ToList()
                                       })
                                       .ToList();

      // Convert to selection view models
      return groupedShifts.Select(g => new DailyShiftSelectionViewModel
      {
        Date = g.Date,
        SelectedShiftIds = g.ShiftIds
      }).ToList();
    }
  }
}
