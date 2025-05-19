using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.CraneUsage;
using AspnetCoreMvcFull.ViewModels.CraneUsage;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Filters;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize]
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class CraneUsageController : Controller
  {
    private readonly ICraneUsageService _craneUsageService;
    private readonly AppDbContext _context;
    private readonly ILogger<CraneUsageController> _logger;

    public CraneUsageController(
        ICraneUsageService craneUsageService,
        AppDbContext context,
        ILogger<CraneUsageController> logger)
    {
      _craneUsageService = craneUsageService;
      _context = context;
      _logger = logger;
    }

    // GET: CraneUsage
    public async Task<IActionResult> Index(CraneUsageFilterViewModel filter)
    {
      try
      {
        var recordsViewModel = await _craneUsageService.GetFilteredUsageRecordsAsync(filter);

        // Menggunakan ViewBag untuk pesan dari TempData
        ViewBag.SuccessMessage = TempData["CraneUsageSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["CraneUsageErrorMessage"] as string;

        // Hapus TempData setelah digunakan
        TempData.Remove("CraneUsageSuccessMessage");
        TempData.Remove("CraneUsageErrorMessage");

        return View(recordsViewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving crane usage records");
        TempData["CraneUsageErrorMessage"] = "Terjadi kesalahan saat mengambil data penggunaan crane: " + ex.Message;
        return View(new CraneUsageRecordListViewModel());
      }
    }

    // POST: CraneUsage/SelectCraneAndDate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectCraneAndDate(int craneId, DateTime date)
    {
      if (craneId <= 0)
      {
        TempData["CraneUsageErrorMessage"] = "Silakan pilih crane yang valid.";
        return RedirectToAction(nameof(Index));
      }

      return RedirectToAction(nameof(Form), new { craneId, date = date.ToString("yyyy-MM-dd") });
    }

    // GET: CraneUsage/Form
    public async Task<IActionResult> Form(int craneId, DateTime date)
    {
      try
      {
        // Get the crane
        var crane = await _context.Cranes.FindAsync(craneId);
        if (crane == null)
        {
          TempData["CraneUsageErrorMessage"] = "Crane tidak ditemukan.";
          return RedirectToAction(nameof(Index));
        }

        // Get or create the usage record
        var record = await _context.CraneUsageRecords
            .FirstOrDefaultAsync(r => r.CraneId == craneId && r.Date.Date == date.Date);

        // Get entries for this crane and date
        var entries = await _craneUsageService.GetCraneUsageEntriesForDateAsync(craneId, date);

        // Create view model
        var viewModel = new CraneUsageFormViewModel
        {
          CraneId = craneId,
          CraneCode = crane.Code,
          Date = date,
          Entries = entries,
          IsFinalized = record?.IsFinalized ?? false,
          FinalizedBy = record?.FinalizedBy,
          FinalizedAt = record?.FinalizedAt
        };

        ViewBag.SuccessMessage = TempData["CraneUsageSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["CraneUsageErrorMessage"] as string;

        TempData.Remove("CraneUsageSuccessMessage");
        TempData.Remove("CraneUsageErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading crane usage form");
        TempData["CraneUsageErrorMessage"] = "Error loading crane usage form: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // POST: CraneUsage/Form
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Form(CraneUsageFormViewModel viewModel)
    {
      try
      {
        // Get current user info
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        // Save the form
        var result = await _craneUsageService.SaveCraneUsageFormAsync(viewModel, userName);

        if (result)
        {
          TempData["CraneUsageSuccessMessage"] = "Data penggunaan crane berhasil disimpan.";
        }
        else
        {
          TempData["CraneUsageErrorMessage"] = "Error menyimpan penggunaan crane. Mohon periksa apakah ada konflik waktu.";
        }

        return RedirectToAction(nameof(Form), new { craneId = viewModel.CraneId, date = viewModel.Date.ToString("yyyy-MM-dd") });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saving crane usage form");
        TempData["CraneUsageErrorMessage"] = "Error menyimpan penggunaan crane: " + ex.Message;
        return RedirectToAction(nameof(Form), new { craneId = viewModel.CraneId, date = viewModel.Date.ToString("yyyy-MM-dd") });
      }
    }

    // GET: CraneUsage/Finalize
    [HttpGet]
    public async Task<IActionResult> Finalize(int craneId, DateTime date)
    {
      try
      {
        // Get current user info
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        // Finalize the record
        var result = await _craneUsageService.FinalizeRecordAsync(craneId, date, userName);

        if (result)
        {
          TempData["CraneUsageSuccessMessage"] = "Record berhasil difinalisasi.";
        }
        else
        {
          TempData["CraneUsageErrorMessage"] = "Gagal finalisasi record. Pastikan ada entries untuk tanggal ini.";
        }

        return RedirectToAction(nameof(Form), new { craneId, date = date.ToString("yyyy-MM-dd") });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error finalizing record for crane ID {CraneId} on date {Date}", craneId, date);
        TempData["CraneUsageErrorMessage"] = "Error finalisasi record: " + ex.Message;
        return RedirectToAction(nameof(Form), new { craneId, date = date.ToString("yyyy-MM-dd") });
      }
    }

    // AJAX endpoints for the Form page
    [HttpPost]
    public async Task<IActionResult> AddEntry(CraneUsageEntryViewModel entry, int craneId, DateTime date)
    {
      try
      {
        // Get current user info
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        // Add the entry
        var result = await _craneUsageService.AddCraneUsageEntryAsync(craneId, date, entry, userName);

        if (result)
        {
          // Get the entry with navigation properties
          var updatedEntry = await _craneUsageService.GetCraneUsageEntryByTimeAsync(craneId, date, entry.StartTime, entry.EndTime);
          return Json(new { success = true, entry = updatedEntry });
        }
        else
        {
          return Json(new { success = false, message = "Gagal menambahkan entry. Mohon periksa apakah ada konflik waktu." });
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error adding crane usage entry");
        return Json(new { success = false, message = "Error menambahkan entry: " + ex.Message });
      }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEntry([FromBody] CraneUsageEntryViewModel entry)
    {
      try
      {
        // Update the entry
        var result = await _craneUsageService.UpdateCraneUsageEntryAsync(entry);

        if (result)
        {
          // Get the updated entry
          var updatedEntry = await _craneUsageService.GetCraneUsageEntryByIdAsync(entry.Id);
          return Json(new { success = true, entry = updatedEntry });
        }
        else
        {
          return Json(new { success = false, message = "Gagal memperbarui entry. Mohon periksa apakah ada konflik waktu." });
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating crane usage entry");
        return Json(new { success = false, message = "Error memperbarui entry: " + ex.Message });
      }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEntry(int id)
    {
      try
      {
        var result = await _craneUsageService.DeleteCraneUsageEntryAsync(id);
        return Json(new { success = result });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting crane usage entry");
        return Json(new { success = false, message = "Error menghapus entry: " + ex.Message });
      }
    }

    // Helper methods for the Form page
    [HttpGet]
    public async Task<IActionResult> GetSubcategories(UsageCategory category)
    {
      try
      {
        var subcategories = await _craneUsageService.GetSubcategoriesByCategoryAsync(category);
        return Json(subcategories);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting subcategories for category {Category}", category);
        return Json(new List<SelectListItem>());
      }
    }

    [HttpGet]
    public async Task<IActionResult> SearchBookings(string term, int craneId)
    {
      try
      {
        var bookings = await _context.Bookings
            .Where(b => b.CraneId == craneId &&
                       (b.Status == BookingStatus.PICApproved ||
                        b.Status == BookingStatus.Done) &&
                       (b.BookingNumber.Contains(term) ||
                        b.DocumentNumber.Contains(term) ||
                        b.Name.Contains(term)))
            .Select(b => new SelectListItem
            {
              Value = b.Id.ToString(),
              Text = $"{b.BookingNumber} - {b.Name} ({b.StartDate:dd/MM/yyyy} - {b.EndDate:dd/MM/yyyy})"
            })
            .Take(10)
            .ToListAsync();

        return Json(bookings);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error searching bookings");
        return Json(new List<SelectListItem>());
      }
    }

    // The MinuteVisualization action
    public async Task<IActionResult> MinuteVisualization(int craneId = 0, DateTime? date = null)
    {
      try
      {
        if (craneId == 0)
        {
          // Default to first crane if none specified
          var firstCrane = await _context.Cranes.OrderBy(c => c.Code).FirstOrDefaultAsync();
          if (firstCrane != null)
          {
            craneId = firstCrane.Id;
          }
        }

        var viewDate = date ?? DateTime.Today;
        var viewModel = await _craneUsageService.GetMinuteVisualizationDataAsync(craneId, viewDate);

        // Messages from TempData
        ViewBag.SuccessMessage = TempData["CraneUsageSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["CraneUsageErrorMessage"] as string;

        // Clear TempData after use
        TempData.Remove("CraneUsageSuccessMessage");
        TempData.Remove("CraneUsageErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving crane usage minute visualization data");
        TempData["CraneUsageErrorMessage"] = "Terjadi kesalahan saat memuat visualisasi penggunaan crane: " + ex.Message;
        return View(new CraneUsageMinuteVisualizationViewModel());
      }
    }
  }
}
