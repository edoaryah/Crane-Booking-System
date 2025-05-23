using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.ViewModels.BookingManagement;
using AspnetCoreMvcFull.Models.Common;
using AspnetCoreMvcFull.Models;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class BookingListController : Controller
  {
    private readonly IBookingService _bookingService;
    private readonly ICraneService _craneService;
    private readonly ILogger<BookingListController> _logger;

    public BookingListController(
        IBookingService bookingService,
        ICraneService craneService,
        ILogger<BookingListController> logger)
    {
      _bookingService = bookingService;
      _craneService = craneService;
      _logger = logger;
    }

    /// <summary>
    /// Main entry point for the booking list page.
    /// </summary>
    public async Task<IActionResult> Index(BookingListFilterRequest filter)
    {
      try
      {
        // Initialize filter if null and ensure valid defaults
        filter ??= new BookingListFilterRequest();

        // Validate pagination parameters
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "SubmitTime";

        // Get dropdown lists
        filter.CraneList = await GetCraneSelectListAsync();
        filter.DepartmentList = await GetDepartmentSelectListAsync();
        filter.StatusList = GetStatusSelectList();

        // Get paged data
        var pagedBookings = await _bookingService.GetPagedBookingsAsync(filter);

        // Get current user from claims
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        // Build view model
        var viewModel = new BookingListPagedViewModel
        {
          PagedBookings = pagedBookings,
          Filter = filter,
          SuccessMessage = TempData["SuccessMessage"] as string,
          ErrorMessage = TempData["ErrorMessage"] as string
        };

        // Pass user info to view
        ViewBag.CurrentUser = userName;

        // Clear TempData after use
        TempData.Remove("SuccessMessage");
        TempData.Remove("ErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking list");

        // Create empty model with error message
        var errorModel = new BookingListPagedViewModel
        {
          PagedBookings = new PagedResult<BookingViewModel>
          {
            Items = Enumerable.Empty<BookingViewModel>(),
            TotalCount = 0,
            PageCount = 0,
            PageNumber = 1,
            PageSize = 10
          },
          Filter = new BookingListFilterRequest
          {
            CraneList = await GetCraneSelectListAsync(),
            DepartmentList = await GetDepartmentSelectListAsync(),
            StatusList = GetStatusSelectList()
          },
          ErrorMessage = "Terjadi kesalahan saat memuat data: " + ex.Message
        };

        return View(errorModel);
      }
    }

    /// <summary>
    /// AJAX endpoint to get just the table portion.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTableData(BookingListFilterRequest filter)
    {
      try
      {
        // Initialize filter if null and ensure valid defaults
        filter ??= new BookingListFilterRequest();

        // Validate pagination parameters
        if (filter.PageNumber < 1) filter.PageNumber = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (string.IsNullOrEmpty(filter.SortBy)) filter.SortBy = "SubmitTime";

        // Get paged data
        var pagedBookings = await _bookingService.GetPagedBookingsAsync(filter);

        // Get dropdown lists (needed for when returning to main view)
        filter.CraneList = await GetCraneSelectListAsync();
        filter.DepartmentList = await GetDepartmentSelectListAsync();
        filter.StatusList = GetStatusSelectList();

        // Build view model for partial
        var viewModel = new BookingListPagedViewModel
        {
          PagedBookings = pagedBookings,
          Filter = filter
        };

        // Return partial view
        return PartialView("_BookingListTable", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking list table data");

        return Content("<div class='alert alert-danger m-3'>" +
                      "<i class='bx bx-error-circle me-2'></i>" +
                      "Terjadi kesalahan saat memuat data: " + ex.Message +
                      "</div>", "text/html");
      }
    }

    /// <summary>
    /// Filter bookings for a specific crane.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Crane(int craneId)
    {
      try
      {
        var crane = await _craneService.GetCraneByIdAsync(craneId);
        if (crane == null)
        {
          return NotFound("Crane not found");
        }

        // Create filter specifically for this crane
        var filter = new BookingListFilterRequest
        {
          CraneId = craneId,
          PageNumber = 1,
          PageSize = 10,
          SortBy = "SubmitTime",
          SortDesc = true,
          CraneList = await GetCraneSelectListAsync(),
          DepartmentList = await GetDepartmentSelectListAsync(),
          StatusList = GetStatusSelectList()
        };

        // Get bookings
        var pagedBookings = await _bookingService.GetPagedBookingsAsync(filter);

        var viewModel = new BookingListPagedViewModel
        {
          PagedBookings = pagedBookings,
          Filter = filter,
          SuccessMessage = TempData["SuccessMessage"] as string,
          ErrorMessage = TempData["ErrorMessage"] as string
        };

        // Clear TempData after use
        TempData.Remove("SuccessMessage");
        TempData.Remove("ErrorMessage");

        // Add crane info to ViewBag for display
        ViewBag.CraneName = crane.Code;
        ViewBag.CraneId = craneId;

        return View("Index", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking list for crane ID: {CraneId}", craneId);
        TempData["ErrorMessage"] = "Error loading booking list: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    /// <summary>
    /// Helper methods for dropdown lists.
    /// </summary>
    private async Task<List<SelectListItem>> GetCraneSelectListAsync()
    {
      var cranes = await _craneService.GetAllCranesAsync();
      return cranes.Select(c => new SelectListItem
      {
        Value = c.Id.ToString(),
        Text = $"{c.Code} - {c.Capacity} ton"
      }).ToList();
    }

    private async Task<List<SelectListItem>> GetDepartmentSelectListAsync()
    {
      var departments = await _bookingService.GetDistinctDepartmentsAsync();
      return departments.Select(d => new SelectListItem
      {
        Value = d,
        Text = d
      }).ToList();
    }

    private List<SelectListItem> GetStatusSelectList()
    {
      return Enum.GetValues<BookingStatus>()
          .Select(s => new SelectListItem
          {
            Value = ((int)s).ToString(),
            Text = GetStatusDisplayName(s)
          }).ToList();
    }

    private string GetStatusDisplayName(BookingStatus status)
    {
      return status switch
      {
        BookingStatus.PendingApproval => "Menunggu Persetujuan",
        BookingStatus.ManagerApproved => "Disetujui Manager",
        BookingStatus.ManagerRejected => "Ditolak Manager",
        BookingStatus.PICApproved => "Disetujui PIC",
        BookingStatus.PICRejected => "Ditolak PIC",
        BookingStatus.Cancelled => "Dibatalkan",
        BookingStatus.Done => "Selesai",
        _ => status.ToString()
      };
    }
  }
}
