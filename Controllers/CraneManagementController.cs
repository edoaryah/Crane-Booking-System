// Controllers/CraneManagementController.cs (Updated)
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.CraneManagement;
using AspnetCoreMvcFull.Models; // Added for CraneStatus enum

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class CraneManagementController : Controller
  {
    private readonly ICraneService _craneService;

    public CraneManagementController(ICraneService craneService)
    {
      _craneService = craneService;
    }

    public async Task<IActionResult> Index()
    {
      var cranes = await _craneService.GetAllCranesAsync();
      return View(cranes);
    }

    public async Task<IActionResult> Details(int id)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(id);
        return View(crane);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
    }

    public IActionResult Create()
    {
      return View(new CraneCreateViewModel { Code = string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CraneCreateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        try
        {
          await _craneService.CreateCraneAsync(viewModel);
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"Error creating crane: {ex.Message}");
        }
      }
      return View(viewModel);
    }

    public async Task<IActionResult> Edit(int id)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(id);
        var viewModel = new CraneUpdateViewModel
        {
          Code = crane.Code,
          Capacity = crane.Capacity,
          Status = crane.Status,
          Ownership = crane.Ownership
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
    public async Task<IActionResult> Edit(int id, CraneUpdateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        try
        {
          var updateModel = new CraneUpdateWithBreakdownViewModel
          {
            Crane = viewModel
          };
          await _craneService.UpdateCraneAsync(id, updateModel);
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"Error updating crane: {ex.Message}");
        }
      }
      return View(viewModel);
    }

    public async Task<IActionResult> Delete(int id)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(id);
        return View(crane);
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
        await _craneService.DeleteCraneAsync(id);
        return RedirectToAction(nameof(Index));
      }
      catch (Exception)
      {
        return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Breakdown(int id, BreakdownCreateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        try
        {
          // Get the existing crane info first
          var crane = await _craneService.GetCraneByIdAsync(id);

          var craneUpdate = new CraneUpdateWithBreakdownViewModel
          {
            Crane = new CraneUpdateViewModel
            {
              Code = crane.Code,
              Capacity = crane.Capacity,
              Ownership = crane.Ownership,
              Status = CraneStatus.Maintenance
            },
            Breakdown = viewModel
          };

          await _craneService.UpdateCraneAsync(id, craneUpdate);
          return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"Error setting crane to breakdown: {ex.Message}");
          var crane = await _craneService.GetCraneByIdAsync(id);
          return View("Details", crane);
        }
      }
      var model = await _craneService.GetCraneByIdAsync(id);
      return View("Details", model);
    }

    // Controllers/CraneManagementController.cs
    // Add this method to the existing controller

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAvailable(int id)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(id);

        // Create update model with existing values
        var updateModel = new CraneUpdateWithBreakdownViewModel
        {
          Crane = new CraneUpdateViewModel
          {
            Code = crane.Code,
            Capacity = crane.Capacity,
            Status = Models.CraneStatus.Available,
            Ownership = crane.Ownership
          }
        };

        await _craneService.UpdateCraneAsync(id, updateModel);
        return RedirectToAction(nameof(Details), new { id });
      }
      catch (Exception ex)
      {
        ModelState.AddModelError("", $"Error setting crane to available: {ex.Message}");
        var crane = await _craneService.GetCraneByIdAsync(id);
        return View("Details", crane);
      }
    }
  }
}
