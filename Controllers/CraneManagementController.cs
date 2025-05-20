// Controllers/CraneManagementController.cs (Updated with error & success messages)
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
    private readonly ILogger<CraneManagementController> _logger;

    public CraneManagementController(ICraneService craneService, ILogger<CraneManagementController> logger)
    {
      _craneService = craneService;
      _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
      try
      {
        var cranes = await _craneService.GetAllCranesAsync();

        // Display messages from TempData
        ViewBag.SuccessMessage = TempData["CraneSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["CraneErrorMessage"] as string;

        // Remove TempData after use to prevent messages reappearing on refresh
        TempData.Remove("CraneSuccessMessage");
        TempData.Remove("CraneErrorMessage");

        return View(cranes);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading cranes");
        ViewBag.ErrorMessage = "Error loading cranes: " + ex.Message;
        return View(new List<CraneViewModel>());
      }
    }

    public async Task<IActionResult> Details(int id)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(id);

        // Display messages from TempData for the details view
        ViewBag.SuccessMessage = TempData["CraneSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["CraneErrorMessage"] as string;

        // Remove TempData after use
        TempData.Remove("CraneSuccessMessage");
        TempData.Remove("CraneErrorMessage");

        return View(crane);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading crane with ID {id}", id);
        TempData["CraneErrorMessage"] = "Error loading crane: " + ex.Message;
        return RedirectToAction(nameof(Index));
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
          TempData["CraneSuccessMessage"] = "Crane created successfully";
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error creating crane");
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
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading crane with ID {id} for edit", id);
        TempData["CraneErrorMessage"] = "Error loading crane for edit: " + ex.Message;
        return RedirectToAction(nameof(Index));
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
          TempData["CraneSuccessMessage"] = "Crane updated successfully";
          return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error updating crane with ID {id}", id);
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
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading crane with ID {id} for deletion", id);
        TempData["CraneErrorMessage"] = "Error loading crane for deletion: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      try
      {
        await _craneService.DeleteCraneAsync(id);
        TempData["CraneSuccessMessage"] = "Crane deleted successfully";
        return RedirectToAction(nameof(Index));
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting crane with ID {id}", id);
        // Use ViewBag for error messages and stay on Delete page
        var crane = await _craneService.GetCraneByIdAsync(id);
        ViewBag.ErrorMessage = "Error deleting crane: " + ex.Message;
        return View(crane);
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
          TempData["CraneSuccessMessage"] = "Crane status set to maintenance successfully";
          return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error setting crane with ID {id} to breakdown", id);
          ModelState.AddModelError("", $"Error setting crane to breakdown: {ex.Message}");
          var crane = await _craneService.GetCraneByIdAsync(id);
          return View("Details", crane);
        }
      }

      var model = await _craneService.GetCraneByIdAsync(id);
      return View("Details", model);
    }

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
        TempData["CraneSuccessMessage"] = "Crane status set to available successfully";
        return RedirectToAction(nameof(Details), new { id });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error setting crane with ID {id} to available", id);
        ModelState.AddModelError("", $"Error setting crane to available: {ex.Message}");
        var crane = await _craneService.GetCraneByIdAsync(id);
        return View("Details", crane);
      }
    }
  }
}
