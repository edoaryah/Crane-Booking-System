using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.UsageManagement;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class UsageManagementController : Controller
  {
    private readonly IUsageSubcategoryService _usageService;
    private readonly ILogger<UsageManagementController> _logger;

    public UsageManagementController(IUsageSubcategoryService usageService, ILogger<UsageManagementController> logger)
    {
      _usageService = usageService;
      _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
      try
      {
        var subcategories = await _usageService.GetAllUsageSubcategoriesAsync();

        var viewModel = new UsageSubcategoryListViewModel
        {
          Subcategories = subcategories
        };

        // Tampilkan pesan dari TempData
        ViewBag.SuccessMessage = TempData["UsageSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["UsageErrorMessage"] as string;

        // Hapus TempData setelah digunakan, ini akan mencegah
        // pesan muncul kembali saat halaman di-refresh
        TempData.Remove("UsageSuccessMessage");
        TempData.Remove("UsageErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading usage subcategories");
        ViewBag.ErrorMessage = "Error loading usage subcategories: " + ex.Message;
        return View(new UsageSubcategoryListViewModel());
      }
    }

    public IActionResult Create()
    {
      return View(new UsageSubcategoryCreateViewModel { Name = string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsageSubcategoryCreateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        try
        {
          await _usageService.CreateUsageSubcategoryAsync(viewModel);

          TempData["UsageSuccessMessage"] = "Usage subcategory created successfully";
          return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
          ModelState.AddModelError("", ex.Message);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error creating usage subcategory");
          ModelState.AddModelError("", $"Error creating usage subcategory: {ex.Message}");
        }
      }

      return View(viewModel);
    }

    public async Task<IActionResult> Edit(int id)
    {
      try
      {
        var subcategory = await _usageService.GetUsageSubcategoryByIdAsync(id);

        var viewModel = new UsageSubcategoryUpdateViewModel
        {
          Name = subcategory.Name,
          Category = subcategory.Category,
          Description = subcategory.Description,
          IsActive = subcategory.IsActive
        };

        return View(viewModel);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading usage subcategory with ID {id}", id);
        TempData["UsageErrorMessage"] = "Error loading usage subcategory: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsageSubcategoryUpdateViewModel viewModel)
    {
      if (ModelState.IsValid)
      {
        try
        {
          await _usageService.UpdateUsageSubcategoryAsync(id, viewModel);

          TempData["UsageSuccessMessage"] = "Usage subcategory updated successfully";
          return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
          return NotFound();
        }
        catch (InvalidOperationException ex)
        {
          ModelState.AddModelError("", ex.Message);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error updating usage subcategory with ID {id}", id);
          ModelState.AddModelError("", $"Error updating usage subcategory: {ex.Message}");
        }
      }

      return View(viewModel);
    }

    // Updated Delete method - tidak perlu menggunakan TempData untuk error
    public async Task<IActionResult> Delete(int id)
    {
      try
      {
        var subcategory = await _usageService.GetUsageSubcategoryByIdAsync(id);
        return View(subcategory);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading usage subcategory with ID {id} for deletion", id);
        TempData["UsageErrorMessage"] = "Error loading usage subcategory: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      try
      {
        await _usageService.DeleteUsageSubcategoryAsync(id);
        TempData["UsageSuccessMessage"] = "Usage subcategory deleted successfully";
        return RedirectToAction(nameof(Index));
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (InvalidOperationException ex)
      {
        // Gunakan ViewBag untuk pesan error dan tetap di halaman Delete
        // Tidak menggunakan TempData agar pesan tidak muncul di halaman Index
        var subcategory = await _usageService.GetUsageSubcategoryByIdAsync(id);
        ViewBag.ErrorMessage = ex.Message;
        return View(subcategory);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting usage subcategory with ID {id}", id);
        // Gunakan ViewBag untuk pesan error dan tetap di halaman Delete
        var subcategory = await _usageService.GetUsageSubcategoryByIdAsync(id);
        ViewBag.ErrorMessage = "Error deleting usage subcategory: " + ex.Message;
        return View(subcategory);
      }
    }

    public async Task<IActionResult> Details(int id)
    {
      try
      {
        var subcategory = await _usageService.GetUsageSubcategoryByIdAsync(id);
        return View(subcategory);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading usage subcategory details for ID {id}", id);
        TempData["UsageErrorMessage"] = "Error loading usage subcategory details: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }
  }
}
