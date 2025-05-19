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

    // GET: /Billing/Details/5
    public async Task<IActionResult> Details(int id)
    {
      try
      {
        var viewModel = await _billingService.GetBillingDetailAsync(id);

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
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving billing details for ID {id}", id);
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat mengambil detail penagihan: " + ex.Message;
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

        return RedirectToAction(nameof(Details), new { id = viewModel.BookingId });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error marking booking as billed");
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat menandai booking sebagai sudah ditagih: " + ex.Message;
        return RedirectToAction(nameof(Details), new { id = viewModel.BookingId });
      }
    }

    // POST: /Billing/UnmarkAsBilled/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkAsBilled(int id)
    {
      try
      {
        // Batalkan status sudah ditagih
        var result = await _billingService.UnmarkBookingAsBilledAsync(id);

        if (result)
        {
          TempData["BillingSuccessMessage"] = "Booking berhasil dibatalkan status penagihan";
        }
        else
        {
          TempData["BillingErrorMessage"] = "Gagal membatalkan status penagihan booking";
        }

        return RedirectToAction(nameof(Details), new { id });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error unmarking booking as billed");
        TempData["BillingErrorMessage"] = "Terjadi kesalahan saat membatalkan status penagihan booking: " + ex.Message;
        return RedirectToAction(nameof(Details), new { id });
      }
    }
  }
}
