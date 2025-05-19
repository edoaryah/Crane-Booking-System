using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using AspnetCoreMvcFull.ViewModels.BookingManagement;
using AspnetCoreMvcFull.Helpers;

namespace AspnetCoreMvcFull.Controllers
{
  [Authorize]
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class BookingHistoryController : Controller
  {
    private readonly IBookingService _bookingService;
    private readonly IRoleService _roleService;
    private readonly ILogger<BookingHistoryController> _logger;
    private readonly ICraneService _craneService;

    public BookingHistoryController(
        IBookingService bookingService,
        IRoleService roleService,
        ILogger<BookingHistoryController> logger,
        ICraneService craneService)
    {
      _bookingService = bookingService;
      _roleService = roleService;
      _logger = logger;
      _craneService = craneService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
      try
      {
        // Get user data from claims
        var ldapUser = User.FindFirst("ldapuser")?.Value;
        if (string.IsNullOrEmpty(ldapUser))
        {
          _logger.LogWarning("User tidak memiliki identitas LDAP. Mengarahkan ke halaman login");
          return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Index", "BookingHistory") });
        }

        var viewModel = new BookingHistoryViewModel
        {
          AvailableCranes = await _craneService.GetAllCranesAsync()
        };

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading data for BookingHistory/Index");
        return View(new BookingHistoryViewModel());
      }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
      try
      {
        // Get the current user's information from claims
        var ldapUser = User.FindFirst("ldapuser")?.Value;

        // Nama pengguna lebih baik diambil dari Claim.Name yang lebih standar
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(ldapUser))
        {
          _logger.LogWarning("User LDAP username not found in claims");
          return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Details", "BookingHistory", new { id }) });
        }

        // Get booking details
        var booking = await _bookingService.GetBookingByIdAsync(id);

        // Cek otorisasi: Apakah pengguna memiliki akses ke booking ini
        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {id} tidak ditemukan", id);
          return NotFound();
        }

        // Cek otorisasi: Apakah pengguna adalah PIC, atau pembuat booking, atau punya akses admin
        bool isPic = await _roleService.UserHasRoleAsync(ldapUser, "pic");
        bool isAdmin = await _roleService.UserHasRoleAsync(ldapUser, "admin");
        bool isBookingCreator = booking.Name == userName;

        // Jika bukan PIC, admin, atau pembuat booking, tolak akses
        if (!isPic && !isAdmin && !isBookingCreator)
        {
          _logger.LogWarning("User {ldapUser} mencoba mengakses booking {id} tanpa otorisasi", ldapUser, id);
          return RedirectToAction("AccessDenied", "Auth");
        }

        // Pass role information to the view
        ViewData["IsPicRole"] = isPic;
        ViewData["IsAdminRole"] = isAdmin;
        ViewData["IsBookingCreator"] = isBookingCreator;

        // Pass the booking to the view
        return View(booking);
      }
      catch (KeyNotFoundException ex)
      {
        _logger.LogWarning(ex, "Booking dengan ID {id} tidak ditemukan", id);
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Terjadi kesalahan saat memuat detail booking dengan ID {id}", id);
        TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat detail booking. Silakan coba lagi.";
        return View(null); // Send null model, view will display error message
      }
    }

    [HttpGet]
    public async Task<IActionResult> Approved()
    {
      // Verifikasi bahwa pengguna memiliki peran PIC atau admin
      var ldapUser = User.FindFirst("ldapuser")?.Value;
      if (string.IsNullOrEmpty(ldapUser))
      {
        return RedirectToAction("Login", "Auth");
      }

      // Gunakan helper untuk memeriksa peran
      bool isPicOrAdmin = await AuthorizationHelper.HasRole(User, _roleService, "pic") ||
                        await AuthorizationHelper.HasRole(User, _roleService, "admin");

      // Jika bukan PIC atau admin, tolak akses
      if (!isPicOrAdmin)
      {
        return RedirectToAction("AccessDenied", "Auth");
      }

      ViewData["Title"] = "Approved Bookings";
      return View();
    }
  }
}
