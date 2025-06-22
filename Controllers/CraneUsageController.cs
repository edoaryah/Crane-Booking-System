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
using AspnetCoreMvcFull.Models.Common;

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
    // Ganti method Index yang ada dengan ini:
    public async Task<IActionResult> Index(CraneUsagePagedRequest filter)
    {
      try
      {
        // Initialize filter if null and ensure valid defaults
        filter ??= new CraneUsagePagedRequest();

        // Validate pagination parameters
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "Date";

        // Set default date range if not provided
        // if (!filter.StartDate.HasValue && !filter.EndDate.HasValue)
        // {
        //   filter.StartDate = DateTime.Today.AddDays(-30);
        //   filter.EndDate = DateTime.Today;
        // }

        // Get crane list for dropdown
        filter.CraneList = await _context.Cranes
            .OrderBy(c => c.Code)
            .Select(c => new SelectListItem
            {
              Value = c.Id.ToString(),
              Text = $"{c.Code} - {c.Capacity} Ton"
            })
            .ToListAsync();

        // Get status list for dropdown
        filter.StatusList = new List<SelectListItem>
    {
      new SelectListItem { Value = "true", Text = "Sudah Difinalisasi" },
      new SelectListItem { Value = "false", Text = "Belum Difinalisasi" }
    };

        // Get paged data
        var pagedRecords = await _craneUsageService.GetPagedCraneUsageRecordsAsync(filter);

        // Get current user from claims
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        // Build view model
        var viewModel = new CraneUsageHistoryPagedViewModel
        {
          PagedRecords = pagedRecords,
          Filter = filter,
          SuccessMessage = TempData["CraneUsageSuccessMessage"] as string,
          ErrorMessage = TempData["CraneUsageErrorMessage"] as string
        };

        // Pass user info to view
        ViewBag.CurrentUser = userName;

        // Clear TempData after use
        TempData.Remove("CraneUsageSuccessMessage");
        TempData.Remove("CraneUsageErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading crane usage records");

        // Create empty model with error message
        var errorModel = new CraneUsageHistoryPagedViewModel
        {
          PagedRecords = new PagedResult<CraneUsageRecordViewModel>
          {
            Items = Enumerable.Empty<CraneUsageRecordViewModel>(),
            TotalCount = 0,
            PageCount = 0,
            PageNumber = 1,
            PageSize = 10
          },
          Filter = new CraneUsagePagedRequest
          {
            CraneList = await _context.Cranes.Select(c => new SelectListItem
            {
              Value = c.Id.ToString(),
              Text = $"{c.Code} - {c.Capacity} Ton"
            }).ToListAsync(),
            StatusList = new List<SelectListItem>
        {
          new SelectListItem { Value = "true", Text = "Sudah Difinalisasi" },
          new SelectListItem { Value = "false", Text = "Belum Difinalisasi" }
        }
          },
          ErrorMessage = "Terjadi kesalahan saat memuat data: " + ex.Message
        };

        return View(errorModel);
      }
    }

    // Tambahkan method baru ini:
    /// <summary>
    /// AJAX endpoint to get just the table portion of the page.
    /// Used for filtering, pagination, etc.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTableData(CraneUsagePagedRequest filter)
    {
      try
      {
        // Initialize filter if null and ensure valid defaults
        filter ??= new CraneUsagePagedRequest();

        // Validate pagination parameters
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "Date";

        // Get paged data
        var pagedRecords = await _craneUsageService.GetPagedCraneUsageRecordsAsync(filter);

        // Get crane list for dropdown (needed for when returning to main view)
        filter.CraneList = await _context.Cranes
            .OrderBy(c => c.Code)
            .Select(c => new SelectListItem
            {
              Value = c.Id.ToString(),
              Text = $"{c.Code} - {c.Capacity} Ton"
            })
            .ToListAsync();

        // Get status list for dropdown
        filter.StatusList = new List<SelectListItem>
    {
      new SelectListItem { Value = "true", Text = "Sudah Difinalisasi" },
      new SelectListItem { Value = "false", Text = "Belum Difinalisasi" }
    };

        // Build view model for partial
        var viewModel = new CraneUsageHistoryPagedViewModel
        {
          PagedRecords = pagedRecords,
          Filter = filter
        };

        // Return partial view
        return PartialView("_CraneUsageTable", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading crane usage table data");

        // Return error message that will be displayed in the table area
        return Content("<div class='alert alert-danger m-3'>" +
                      "<i class='bx bx-error-circle me-2'></i>" +
                      "Terjadi kesalahan saat memuat data: " + ex.Message +
                      "</div>", "text/html");
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

// Get bookings within ±5 days of selected date for dropdown
[HttpGet]
public async Task<IActionResult> NearbyBookings(int craneId, DateTime date)
{
  try
  {
    var startDate = date.AddDays(-5);
    var endDate = date.AddDays(5);

    var bookings = await _context.Bookings
        .Where(b => b.CraneId == craneId &&
                    (b.Status == BookingStatus.PICApproved || b.Status == BookingStatus.Done) &&
                    b.StartDate <= endDate && b.EndDate >= startDate)
        .OrderBy(b => b.StartDate)
        .Select(b => new SelectListItem
        {
          Value = b.Id.ToString(),
          Text = $"{b.BookingNumber} - {b.Name} ({b.StartDate:dd/MM/yyyy} - {b.EndDate:dd/MM/yyyy})"
        })
        .Take(50)
        .ToListAsync();

    return Json(bookings);
  }
  catch (Exception ex)
  {
    _logger.LogError(ex, "Error getting nearby bookings");
    return Json(new List<SelectListItem>());
  }
}
    // The Visualization action
    public async Task<IActionResult> Visualization(int craneId = 0, DateTime? date = null)
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
        var viewModel = await _craneUsageService.GetVisualizationDataAsync(craneId, viewDate);

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
        return View(new CraneUsageVisualizationViewModel());
      }
    }
  }
}
