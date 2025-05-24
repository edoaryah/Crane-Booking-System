// Controllers/BookingActionController.cs
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using AspnetCoreMvcFull.ViewModels.BookingManagement;
using System.Security.Claims;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class BookingActionController : Controller
  {
    private readonly IBookingService _bookingService;
    private readonly IBookingApprovalService _approvalService;
    private readonly IRoleService _roleService;
    private readonly ICraneService _craneService;
    private readonly IShiftDefinitionService _shiftService;
    private readonly IHazardService _hazardService;
    private readonly ILogger<BookingActionController> _logger;

    public BookingActionController(
        IBookingService bookingService,
        IBookingApprovalService approvalService,
        IRoleService roleService,
        ICraneService craneService,
        IShiftDefinitionService shiftService,
        IHazardService hazardService,
        ILogger<BookingActionController> logger)
    {
      _bookingService = bookingService;
      _approvalService = approvalService;
      _roleService = roleService;
      _craneService = craneService;
      _shiftService = shiftService;
      _hazardService = hazardService;
      _logger = logger;
    }

    // GET: /BookingAction/Edit/{documentNumber}
    [HttpGet]
    public async Task<IActionResult> Edit(string documentNumber)
    {
      try
      {
        var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);

        // Get current user info
        string currentLdapUser = User.FindFirst("ldapuser")?.Value ?? "";
        string currentUserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

        if (string.IsNullOrEmpty(currentLdapUser))
        {
          _logger.LogWarning("User LDAP username not found in claims");
          return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Edit", "BookingAction", new { documentNumber }) });
        }

        // Check user roles
        bool isPic = await _roleService.UserHasRoleAsync(currentLdapUser, "pic");
        bool isBookingCreator = booking.Name == currentUserName;

        // Authorization check: Only booking creator or PIC can edit
        if (!isBookingCreator && !isPic)
        {
          _logger.LogWarning("User {LdapUser} attempted to edit booking {DocumentNumber} without authorization", currentLdapUser, documentNumber);
          TempData["ErrorMessage"] = "Anda tidak memiliki izin untuk mengedit booking ini.";
          return RedirectToAction("Details", "Booking", new { documentNumber = documentNumber });
        }

        // ✅ UPDATED: Check edit conditions based on role and status
        bool canEdit = false;
        string reasonCannotEdit = "";

        if (isBookingCreator && !isPic)
        {
          // ✅ Creator: Only edit when rejected (UNCHANGED)
          canEdit = booking.Status == BookingStatus.ManagerRejected || booking.Status == BookingStatus.PICRejected;
          if (!canEdit)
          {
            reasonCannotEdit = "Booking hanya dapat diedit ketika mendapat penolakan dari Manager atau PIC.";
          }
        }
        else if (isPic)
        {
          // ✅ PIC: Only edit when ManagerApproved OR PICApproved (CHANGED)
          canEdit = booking.Status == BookingStatus.ManagerApproved || booking.Status == BookingStatus.PICApproved;

          if (!canEdit)
          {
            switch (booking.Status)
            {
              case BookingStatus.PendingApproval:
                reasonCannotEdit = "Booking sedang menunggu approval Manager. Edit tersedia setelah Manager approve.";
                break;
              case BookingStatus.ManagerRejected:
                reasonCannotEdit = "Booking ditolak Manager. Silakan revise melalui user yang mengajukan.";
                break;
              case BookingStatus.PICRejected:
                reasonCannotEdit = "Booking ditolak PIC. Silakan revise melalui user yang mengajukan.";
                break;
              case BookingStatus.Done:
                reasonCannotEdit = "Booking sudah selesai dan tidak dapat diedit lagi.";
                break;
              case BookingStatus.Cancelled:
                reasonCannotEdit = "Booking sudah dibatalkan dan tidak dapat diedit lagi.";
                break;
              default:
                reasonCannotEdit = "Booking tidak dapat diedit pada status saat ini.";
                break;
            }
          }
        }

        if (!canEdit)
        {
          TempData["ErrorMessage"] = reasonCannotEdit;
          return RedirectToAction("Details", "Booking", new { documentNumber = documentNumber });
        }

        // Convert to update view model
        var viewModel = new BookingUpdateViewModel
        {
          Name = booking.Name,
          Department = booking.Department,
          CraneId = booking.CraneId,
          StartDate = booking.StartDate,
          EndDate = booking.EndDate,
          Location = booking.Location,
          ProjectSupervisor = booking.ProjectSupervisor,
          CostCode = booking.CostCode,
          PhoneNumber = booking.PhoneNumber,
          Description = booking.Description,
          CustomHazard = booking.CustomHazard,
          ShiftSelections = ConvertShiftsToSelections(booking),
          Items = booking.Items.Select(i => new BookingItemCreateViewModel
          {
            ItemName = i.ItemName,
            Weight = i.Weight,
            Height = i.Height,
            Quantity = i.Quantity
          }).ToList(),
          HazardIds = booking.SelectedHazards.Select(h => h.Id).ToList()
        };

        // Pass data to view
        ViewBag.Cranes = await _craneService.GetAllCranesAsync();
        ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
        ViewBag.Hazards = await _hazardService.GetAllHazardsAsync();
        ViewBag.DocumentNumber = booking.DocumentNumber;
        ViewBag.BookingId = booking.Id;
        ViewBag.IsPicEdit = isPic;
        ViewBag.IsCreatorEdit = isBookingCreator && !isPic;
        ViewBag.CurrentStatus = booking.Status;

        return View(viewModel);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking for edit with document number: {DocumentNumber}", documentNumber);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat mengambil data booking.";
        return RedirectToAction("Details", "Booking", new { documentNumber = documentNumber });
      }
    }

    // POST: /BookingAction/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string documentNumber, BookingUpdateViewModel viewModel)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          // Reload form data if validation fails
          ViewBag.Cranes = await _craneService.GetAllCranesAsync();
          ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
          ViewBag.Hazards = await _hazardService.GetAllHazardsAsync();
          ViewBag.DocumentNumber = documentNumber;
          return View(viewModel);
        }

        // Get current booking to check permissions again
        var currentBooking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);

        // Get current user info
        string currentLdapUser = User.FindFirst("ldapuser")?.Value ?? "";
        string currentUserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

        // Check user roles
        bool isPic = await _roleService.UserHasRoleAsync(currentLdapUser, "pic");
        bool isBookingCreator = currentBooking.Name == currentUserName;

        // Re-check authorization
        if (!isBookingCreator && !isPic)
        {
          TempData["ErrorMessage"] = "Anda tidak memiliki izin untuk mengedit booking ini.";
          return RedirectToAction("Details", "Booking", new { documentNumber = documentNumber });
        }

        // Update the booking with tracking info
        var updatedBooking = await _bookingService.UpdateBookingAsync(currentBooking.Id, viewModel, currentUserName);

        // Handle post-edit logic based on who edited
        await HandlePostEditLogic(currentBooking.Id, isPic, isBookingCreator, currentUserName);

        TempData["SuccessMessage"] = "Booking berhasil diperbarui.";
        return RedirectToAction("Details", "Booking", new { documentNumber = updatedBooking.DocumentNumber });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating booking with document number: {DocumentNumber}", documentNumber);
        ModelState.AddModelError("", "Error updating booking: " + ex.Message);

        // Reload form data
        ViewBag.Cranes = await _craneService.GetAllCranesAsync();
        ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
        ViewBag.Hazards = await _hazardService.GetAllHazardsAsync();
        ViewBag.DocumentNumber = documentNumber;

        return View(viewModel);
      }
    }

    // Helper method to handle post-edit logic
    private async Task HandlePostEditLogic(int bookingId, bool isPicEdit, bool isCreatorEdit, string editorName)
    {
      try
      {
        if (isCreatorEdit && !isPicEdit)
        {
          // If booking creator edited, submit revision (reset to PendingApproval)
          await _approvalService.ReviseRejectedBookingAsync(bookingId, editorName);
          _logger.LogInformation("Booking {BookingId} revised by creator {EditorName}", bookingId, editorName);
        }
        else if (isPicEdit)
        {
          // If PIC edited, just update tracking info (no status change needed)
          // The UpdateBookingAsync already handles LastModifiedAt and LastModifiedBy
          _logger.LogInformation("Booking {BookingId} edited by PIC {EditorName}", bookingId, editorName);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in post-edit logic for booking {BookingId}", bookingId);
        // Don't throw here, as the main edit was successful
      }
    }

    // POST: /BookingAction/SubmitRevision/{id}
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

          // Get booking to redirect to details page
          var booking = await _bookingService.GetBookingByIdAsync(id);
          return RedirectToAction("Details", "Booking", new { documentNumber = booking.DocumentNumber });
        }
        else
        {
          TempData["ErrorMessage"] = "Gagal mengajukan revisi booking.";
          var booking = await _bookingService.GetBookingByIdAsync(id);
          return RedirectToAction("Edit", new { documentNumber = booking.DocumentNumber });
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error submitting booking revision with ID: {Id}", id);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat mengajukan revisi booking.";

        try
        {
          var booking = await _bookingService.GetBookingByIdAsync(id);
          return RedirectToAction("Edit", new { documentNumber = booking.DocumentNumber });
        }
        catch
        {
          return RedirectToAction("Index", "Booking");
        }
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
          return RedirectToAction("Details", "Booking", new { documentNumber = documentNumber });
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
        return RedirectToAction("Details", "Booking", new { documentNumber = documentNumber });
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

        // Get booking for document number
        var booking = await _bookingService.GetBookingByIdAsync(model.BookingId);

        // Check if user has PIC role
        bool isPic = await _roleService.UserHasRoleAsync(currentUser, "pic");

        // Determine who's cancelling the booking
        BookingCancelledBy cancelledBy = isPic ? BookingCancelledBy.PIC : BookingCancelledBy.User;

        var result = await _approvalService.CancelBookingAsync(
            model.BookingId,
            cancelledBy,
            userName,
            model.CancelReason);

        if (result)
        {
          TempData["SuccessMessage"] = "Booking berhasil dibatalkan.";
          return RedirectToAction("Details", "Booking", new { documentNumber = booking.DocumentNumber });
        }
        else
        {
          TempData["ErrorMessage"] = "Gagal membatalkan booking.";
          return View("Cancel", model);
        }
      }
      catch (KeyNotFoundException)
      {
        TempData["ErrorMessage"] = "Booking tidak ditemukan.";
        return RedirectToAction("Index", "Booking");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error cancelling booking ID: {Id}", model.BookingId);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat membatalkan booking.";
        return View("Cancel", model);
      }
    }

    // Helper method to convert shifts to selections
    private List<DailyShiftSelectionViewModel> ConvertShiftsToSelections(BookingDetailViewModel booking)
    {
      try
      {
        if (booking?.Shifts == null || !booking.Shifts.Any())
        {
          return new List<DailyShiftSelectionViewModel>();
        }

        // Group shifts by date
        var groupedShifts = booking.Shifts
            .GroupBy(s => s.Date.Date)
            .Select(g => new
            {
              Date = g.Key,
              ShiftIds = g.Select(s => s.ShiftDefinitionId).ToList()
            })
            .OrderBy(g => g.Date)
            .ToList();

        // Convert to selection view models
        return groupedShifts.Select(g => new DailyShiftSelectionViewModel
        {
          Date = g.Date,
          SelectedShiftIds = g.ShiftIds
        }).ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error converting shifts to selections for booking {BookingId}", booking?.Id);
        return new List<DailyShiftSelectionViewModel>();
      }
    }
  }
}
