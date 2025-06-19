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
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<BookingActionController> _logger;

    public BookingActionController(
        IBookingService bookingService,
        IBookingApprovalService approvalService,
        IRoleService roleService,
        ICraneService craneService,
        IShiftDefinitionService shiftService,
        IHazardService hazardService,
        IFileStorageService fileStorageService,
        ILogger<BookingActionController> logger)
    {
      _bookingService = bookingService;
      _approvalService = approvalService;
      _roleService = roleService;
      _craneService = craneService;
      _shiftService = shiftService;
      _hazardService = hazardService;
      _fileStorageService = fileStorageService;
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
          ExistingImagePaths = booking.ImagePaths?.ToList() ?? new List<string>(), // Populate existing images
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
        ViewBag.IsPicEdit = isPic;
        ViewBag.IsCreatorEdit = isBookingCreator;
        ViewBag.CurrentStatus = booking.Status;
        ViewBag.DocumentNumber = documentNumber;
        ViewBag.BookingId = booking.Id;

        // Load necessary data for the form
        await PopulateViewBagForEdit();

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
    public async Task<IActionResult> Edit(string documentNumber, BookingUpdateViewModel model)
    {
      // Repopulate ViewBag data and return view if model state is invalid
      if (!ModelState.IsValid)
      {
        // Log ModelState errors for debugging
        var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                 .Select(x => new { x.Key, x.Value.Errors })
                                 .ToArray();
        foreach (var error in errors)
        {
          _logger.LogError($"ModelState Error in Key: {error.Key}");
          foreach (var subError in error.Errors)
          {
            _logger.LogError($"- {subError.ErrorMessage}");
          }
        }

        _logger.LogWarning("Model state is invalid for booking edit {DocumentNumber}.", documentNumber);
        await PopulateViewBagForEdit(); // Ensure dropdowns are repopulated
        ViewBag.DocumentNumber = documentNumber; // Ensure document number is available

        var bookingForState = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);
        ViewBag.CurrentStatus = bookingForState.Status;

        // Preserve the user's image deletion choices on validation failure
        var currentImagePaths = bookingForState.ImagePaths?.ToList() ?? new List<string>();
        if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
        {
            currentImagePaths.RemoveAll(p => model.ImagesToDelete.Contains(p));
        }
        model.ExistingImagePaths = currentImagePaths;

        return View(model);
      }

      try
      {
        // Get current user info
        string currentUser = User.FindFirst("ldapuser")?.Value ?? "";
        string currentUserName = User.FindFirst(ClaimTypes.Name)?.Value ?? currentUser;

        // Get current booking to check permissions and get existing images
        var currentBooking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);
        var finalImagePaths = currentBooking.ImagePaths?.ToList() ?? new List<string>();

        // Check user roles & authorization
        bool isPic = await _roleService.UserHasRoleAsync(currentUser, "pic");
        bool isBookingCreator = currentBooking.Name == currentUserName;
        if (!isBookingCreator && !isPic)
        {
          TempData["ErrorMessage"] = "Anda tidak memiliki izin untuk mengedit booking ini.";
          return RedirectToAction("Details", "Booking", new { documentNumber = documentNumber });
        }

        // 1. Handle Image Deletion
        if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
        {
          foreach (var imagePath in model.ImagesToDelete)
          {
            await _fileStorageService.DeleteFileAsync(imagePath, "booking-images");
            finalImagePaths.Remove(imagePath);
          }
          _logger.LogInformation("Deleted {Count} images for booking {DocumentNumber}", model.ImagesToDelete.Count, documentNumber);
        }

        // 2. Handle New Image Uploads (using NewImages to match the view)
        if (model.NewImages != null && model.NewImages.Any())
        {
          var uploadedImagePaths = new List<string>();
          foreach (var image in model.NewImages)
          {
            if (image.Length > 0)
            {
              var savedPath = await _fileStorageService.SaveFileAsync(image, "booking-images");
              uploadedImagePaths.Add(savedPath);
            }
          }
          finalImagePaths.AddRange(uploadedImagePaths);
          _logger.LogInformation("Uploaded {Count} new images for booking {DocumentNumber}", uploadedImagePaths.Count, documentNumber);
        }

        // Update the booking with tracking info and new image paths
        var updatedBooking = await _bookingService.UpdateBookingAsync(currentBooking.Id, model, currentUserName, finalImagePaths);

        // Handle post-edit logic based on who edited
        await HandlePostEditLogic(currentBooking.Id, isPic, isBookingCreator, currentUserName);

        TempData["SuccessMessage"] = "Booking berhasil diperbarui.";
        return RedirectToAction("Details", "Booking", new { documentNumber = updatedBooking.DocumentNumber });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating booking with document number: {DocumentNumber}", documentNumber);
        ModelState.AddModelError("", "Error updating booking: " + ex.Message);

        TempData["ErrorMessage"] = "Terjadi kesalahan saat memperbarui booking: " + ex.Message;
        await PopulateViewBagForEdit(); // Ensure dropdowns are repopulated
        ViewBag.DocumentNumber = documentNumber;

        // Re-populate existing images for the view in case of an error
        var currentBooking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);
        model.ExistingImagePaths = currentBooking.ImagePaths?.ToList() ?? new List<string>();

        return View(model);
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

    private async Task PopulateViewBagForEdit()
    {
      ViewBag.AvailableCranes = await _craneService.GetAllCranesAsync();
      ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
      ViewBag.Hazards = await _hazardService.GetAllHazardsAsync();
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
