// Controllers/BookingActionController.cs
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class BookingActionController : Controller
  {
    private readonly IBookingService _bookingService;
    private readonly IBookingApprovalService _approvalService;
    private readonly ILogger<BookingActionController> _logger;

    public BookingActionController(
        IBookingService bookingService,
        IBookingApprovalService approvalService,
        ILogger<BookingActionController> logger)
    {
      _bookingService = bookingService;
      _approvalService = approvalService;
      _logger = logger;
    }

    // GET: /BookingAction/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
      try
      {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        // Check if user is authorized to edit this booking
        string currentUser = User.FindFirst("ldapuser")?.Value ?? "";
        if (booking.Name != currentUser)
        {
          return Forbid();
        }

        // Check if booking is in a status that can be edited
        if (booking.Status != BookingStatus.ManagerRejected && booking.Status != BookingStatus.PICRejected)
        {
          TempData["ErrorMessage"] = "Booking tidak dapat diedit karena statusnya saat ini.";
          return RedirectToAction("Details", "BookingHistory", new { id = id });
        }

        // TODO: Create a view model for editing the booking
        // For now, just pass the booking details to the view
        return View(booking);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking for edit with ID: {Id}", id);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat mengambil data booking.";
        return RedirectToAction("Index", "BookingHistory");
      }
    }

    // POST: /BookingAction/SubmitRevision/5
    [HttpPost]
    public async Task<IActionResult> SubmitRevision(int id)
    {
      try
      {
        string currentUser = User.FindFirst("ldapuser")?.Value ?? "";
        string userName = User.FindFirst(ClaimTypes.Name)?.Value ?? currentUser;

        var result = await _approvalService.ReviseRejectedBookingAsync(id, userName);

        if (result)
        {
          TempData["SuccessMessage"] = "Revisi booking berhasil diajukan.";
          return RedirectToAction("Details", "BookingHistory", new { id = id });
        }
        else
        {
          TempData["ErrorMessage"] = "Gagal mengajukan revisi booking.";
          return RedirectToAction("Edit", new { id = id });
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error submitting booking revision with ID: {Id}", id);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat mengajukan revisi booking.";
        return RedirectToAction("Edit", new { id = id });
      }
    }

    // GET: /BookingAction/Cancel
    [HttpGet]
    public async Task<IActionResult> Cancel(string documentNumber)
    {
      try
      {
        var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);

        // Verify booking can be cancelled
        if (booking.Status == BookingStatus.Done || booking.Status == BookingStatus.Cancelled)
        {
          TempData["ErrorMessage"] = "Booking tidak dapat dibatalkan karena statusnya saat ini.";
          return RedirectToAction("Details", "BookingHistory", new { documentNumber = documentNumber });
        }

        // Prepare cancellation view model
        var viewModel = new BookingCancellationViewModel
        {
          BookingId = booking.Id,
          BookingNumber = booking.BookingNumber,
          DocumentNumber = documentNumber
        };

        return View(viewModel);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading cancellation page for booking with document number: {DocumentNumber}", documentNumber);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat halaman pembatalan.";
        return RedirectToAction("Index", "BookingHistory");
      }
    }

    // POST: /BookingAction/ConfirmCancel
    [HttpPost]
    public async Task<IActionResult> ConfirmCancel(BookingCancellationViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View("Cancel", model);
      }

      try
      {
        string currentUser = User.FindFirst("ldapuser")?.Value ?? "";
        string userName = User.FindFirst(ClaimTypes.Name)?.Value ?? currentUser;

        // Determine who's cancelling the booking
        BookingCancelledBy cancelledBy;
        if (User.IsInRole("pic"))
        {
          cancelledBy = BookingCancelledBy.PIC;
        }
        else
        {
          cancelledBy = BookingCancelledBy.User;
        }

        var result = await _approvalService.CancelBookingAsync(
            model.BookingId,
            cancelledBy,
            userName,
            model.CancelReason);

        if (result)
        {
          TempData["SuccessMessage"] = "Booking berhasil dibatalkan.";
          return RedirectToAction("Details", "Booking", new { id = model.BookingId });
        }
        else
        {
          TempData["ErrorMessage"] = "Gagal membatalkan booking.";
          return View("Cancel", model);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error cancelling booking ID: {Id}", model.BookingId);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat membatalkan booking.";
        return View("Cancel", model);
      }
    }
  }
}
