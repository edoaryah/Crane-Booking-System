// Controllers/BillingController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services.Billing;
using AspnetCoreMvcFull.ViewModels.Billing;
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize]
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class BillingController : Controller
  {
    private readonly IBillingService _billingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(IBillingService billingService, ILogger<BillingController> logger)
    {
      _billingService = billingService;
      _logger = logger;
    }

    // GET: /Billing
    public async Task<IActionResult> Index(BillingFilterViewModel filter)
    {
      try
      {
        var viewModel = await _billingService.GetBillableBookingsAsync(filter);

        // Tampilkan pesan dari TempData
        ViewBag.SuccessMessage = TempData["BillingSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["BillingErrorMessage"] as string;

        // Hapus TempData setelah digunakan
        TempData.Remove("BillingSuccessMessage");
        TempData.Remove("BillingErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving billing data");
        ViewBag.ErrorMessage = "Terjadi kesalahan saat mengambil data penagihan: " + ex.Message;
        return View(new BillingListViewModel());
      }
    }

    // GET: /Billing/Details/{documentNumber}
    [Route("Billing/Details/{documentNumber}")]
    public async Task<IActionResult> Details(string documentNumber)
    {
      try
      {
        var viewModel = await _billingService.GetBillingDetailByDocumentNumberAsync(documentNumber);

        // Tampilkan pesan dari TempData
        ViewBag.SuccessMessage = TempData["BillingSuccessMessage"] as string;
        ViewBag.ErrorMessage = TempData["BillingErrorMessage"] as string;

        // Hapus TempData setelah digunakan
        TempData.Remove("BillingSuccessMessage");
        TempData.Remove("BillingErrorMessage");

        return View(viewModel);
      }
      catch (KeyNotFoundException)
      {
        TempData["BillingErrorMessage"] = $"Booking dengan Document Number {documentNumber} tidak ditemukan";
        return RedirectToAction(nameof(Index));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving billing details for Document Number {documentNumber}", documentNumber);
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat mengambil detail penagihan: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // GET: /Billing/Details/{id:int} - Legacy support (redirect to new URL)
    [Route("Billing/Details/{id:int}")]
    public async Task<IActionResult> DetailsById(int id)
    {
      try
      {
        var viewModel = await _billingService.GetBillingDetailAsync(id);
        // Redirect to new URL format using DocumentNumber
        return RedirectToAction(nameof(Details), new { documentNumber = viewModel.Booking.DocumentNumber });
      }
      catch (KeyNotFoundException)
      {
        TempData["BillingErrorMessage"] = $"Booking dengan ID {id} tidak ditemukan";
        return RedirectToAction(nameof(Index));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error redirecting from legacy URL for ID {id}", id);
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat mengakses detail penagihan";
        return RedirectToAction(nameof(Index));
      }
    }

    // POST: /Billing/MarkAsBilled
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsBilled(MarkAsBilledViewModel viewModel)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          TempData["BillingErrorMessage"] = "Data tidak valid. Silakan periksa kembali.";
          return RedirectToAction(nameof(Details), new { documentNumber = viewModel.DocumentNumber });
        }

        // Dapatkan nama pengguna
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("ldapuser")?.Value ?? "system";

        // Tandai sebagai sudah ditagih
        var result = await _billingService.MarkBookingAsBilledAsync(viewModel.BookingId, userName, viewModel.BillingNotes);

        if (result)
        {
          TempData["BillingSuccessMessage"] = $"Booking {viewModel.BookingNumber} berhasil ditandai sebagai sudah ditagih";
        }
        else
        {
          TempData["BillingErrorMessage"] = "Gagal menandai booking sebagai sudah ditagih";
        }

        return RedirectToAction(nameof(Details), new { documentNumber = viewModel.DocumentNumber });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error marking booking as billed for Document Number {documentNumber}", viewModel.DocumentNumber);
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat menandai booking sebagai sudah ditagih: " + ex.Message;
        return RedirectToAction(nameof(Details), new { documentNumber = viewModel.DocumentNumber });
      }
    }

    // POST: /Billing/UnmarkAsBilled/{documentNumber}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Billing/UnmarkAsBilled/{documentNumber}")]
    public async Task<IActionResult> UnmarkAsBilled(string documentNumber)
    {
      try
      {
        // Dapatkan booking details berdasarkan document number
        var viewModel = await _billingService.GetBillingDetailByDocumentNumberAsync(documentNumber);

        // Batalkan status sudah ditagih
        var result = await _billingService.UnmarkBookingAsBilledAsync(viewModel.Booking.BookingId);

        if (result)
        {
          TempData["BillingSuccessMessage"] = $"Booking {viewModel.Booking.BookingNumber} berhasil dibatalkan status penagihan";
        }
        else
        {
          TempData["BillingErrorMessage"] = "Gagal membatalkan status penagihan booking";
        }

        return RedirectToAction(nameof(Details), new { documentNumber });
      }
      catch (KeyNotFoundException)
      {
        TempData["BillingErrorMessage"] = $"Booking dengan Document Number {documentNumber} tidak ditemukan";
        return RedirectToAction(nameof(Index));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error unmarking booking as billed for Document Number {documentNumber}", documentNumber);
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat membatalkan status penagihan booking: " + ex.Message;
        return RedirectToAction(nameof(Details), new { documentNumber });
      }
    }

    // POST: /Billing/UnmarkAsBilled/{id:int} - Legacy support
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Billing/UnmarkAsBilled/{id:int}")]
    public async Task<IActionResult> UnmarkAsBilledById(int id)
    {
      try
      {
        // Get document number first, then redirect to new method
        var viewModel = await _billingService.GetBillingDetailAsync(id);
        return RedirectToAction(nameof(UnmarkAsBilled), new { documentNumber = viewModel.Booking.DocumentNumber });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in legacy UnmarkAsBilled for ID {id}", id);
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat membatalkan status penagihan booking";
        return RedirectToAction(nameof(Index));
      }
    }
  }
}
