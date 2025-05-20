using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.ShiftManagement;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class ShiftManagementController : Controller
  {
    private readonly IShiftDefinitionService _shiftService;
    private readonly ILogger<ShiftManagementController> _logger;

    public ShiftManagementController(IShiftDefinitionService shiftService, ILogger<ShiftManagementController> logger)
    {
      _shiftService = shiftService;
      _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
      try
      {
        var shifts = await _shiftService.GetAllShiftDefinitionsAsync();
        var viewModel = new ShiftListViewModel
        {
          Shifts = shifts
        };

        // Display messages from TempData
        ViewBag.SuccessMessage = TempData["ShiftSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["ShiftErrorMessage"] as string;

        // Remove TempData after use to prevent messages reappearing on refresh
        TempData.Remove("ShiftSuccessMessage");
        TempData.Remove("ShiftErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading shifts");
        ViewBag.ErrorMessage = "Error loading shifts: " + ex.Message;
        return View(new ShiftListViewModel());
      }
    }

    public async Task<IActionResult> Details(int id)
    {
      try
      {
        var shift = await _shiftService.GetShiftDefinitionByIdAsync(id);
        return View(shift);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading shift with ID {id}", id);
        TempData["ShiftErrorMessage"] = "Error loading shift: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    public IActionResult Create()
    {
      var viewModel = new ShiftCreateViewModel
      {
        Name = string.Empty,
        StartTime = new TimeSpan(7, 0, 0), // Default: 7:00 AM
        EndTime = new TimeSpan(15, 0, 0),  // Default: 3:00 PM
        IsActive = true
      };
      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShiftCreateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        try
        {
          await _shiftService.CreateShiftDefinitionAsync(viewModel);
          TempData["ShiftSuccessMessage"] = "Shift created successfully";
          return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
          ModelState.AddModelError("", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
          ModelState.AddModelError("", ex.Message);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error creating shift");
          ModelState.AddModelError("", $"Error creating shift: {ex.Message}");
        }
      }
      return View(viewModel);
    }

    public async Task<IActionResult> Edit(int id)
    {
      try
      {
        var shift = await _shiftService.GetShiftDefinitionByIdAsync(id);
        var viewModel = new ShiftUpdateViewModel
        {
          Name = shift.Name,
          StartTime = shift.StartTime,
          EndTime = shift.EndTime,
          IsActive = shift.IsActive
        };
        return View(viewModel);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading shift with ID {id} for edit", id);
        TempData["ShiftErrorMessage"] = "Error loading shift for edit: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ShiftUpdateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        try
        {
          await _shiftService.UpdateShiftDefinitionAsync(id, viewModel);
          TempData["ShiftSuccessMessage"] = "Shift updated successfully";
          return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
          return NotFound();
        }
        catch (ArgumentException ex)
        {
          ModelState.AddModelError("", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
          ModelState.AddModelError("", ex.Message);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error updating shift with ID {id}", id);
          ModelState.AddModelError("", $"Error updating shift: {ex.Message}");
        }
      }
      return View(viewModel);
    }

    public async Task<IActionResult> Delete(int id)
    {
      try
      {
        var shift = await _shiftService.GetShiftDefinitionByIdAsync(id);
        return View(shift);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading shift with ID {id} for deletion", id);
        TempData["ShiftErrorMessage"] = "Error loading shift for deletion: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      try
      {
        await _shiftService.DeleteShiftDefinitionAsync(id);
        TempData["ShiftSuccessMessage"] = "Shift deleted successfully";
        return RedirectToAction(nameof(Index));
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (InvalidOperationException ex)
      {
        // Use ViewBag for error messages and stay on Delete page
        // Don't use TempData so the message doesn't appear on the Index page
        var shift = await _shiftService.GetShiftDefinitionByIdAsync(id);
        ViewBag.ErrorMessage = ex.Message;
        return View(shift);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting shift with ID {id}", id);
        // Use ViewBag for error messages and stay on Delete page
        var shift = await _shiftService.GetShiftDefinitionByIdAsync(id);
        ViewBag.ErrorMessage = "Error deleting shift: " + ex.Message;
        return View(shift);
      }
    }
  }
}
