using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.ShiftManagement;
using AspnetCoreMvcFull.Models; // Added for CraneStatus enum

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class ShiftManagementController : Controller
  {
    private readonly IShiftDefinitionService _shiftService;

    public ShiftManagementController(IShiftDefinitionService shiftService)
    {
      _shiftService = shiftService;
    }

    public async Task<IActionResult> Index()
    {
      var shifts = await _shiftService.GetAllShiftDefinitionsAsync();
      var viewModel = new ShiftListViewModel
      {
        Shifts = shifts
      };
      return View(viewModel);
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
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      try
      {
        await _shiftService.DeleteShiftDefinitionAsync(id);
        return RedirectToAction(nameof(Index));
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (InvalidOperationException ex)
      {
        TempData["ErrorMessage"] = ex.Message;
        return RedirectToAction(nameof(Delete), new { id });
      }
      catch (Exception)
      {
        return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
      }
    }
  }
}
