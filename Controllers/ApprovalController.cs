using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.Role;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.ViewModels;
using System.Text;

namespace AspnetCoreMvcFull.Controllers
{
  public class ApprovalController : Controller
  {
    private readonly IBookingService _bookingService;
    private readonly IBookingApprovalService _approvalService;
    private readonly IEmployeeService _employeeService;
    private readonly IRoleService _roleService; // ✅ Tambahkan
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(
        IBookingService bookingService,
        IBookingApprovalService approvalService,
        IEmployeeService employeeService,
        IRoleService roleService,
        ILogger<ApprovalController> logger)
    {
      _bookingService = bookingService;
      _approvalService = approvalService;
      _employeeService = employeeService;
      _roleService = roleService;
      _logger = logger;
    }

    // Halaman approval untuk Manager
    [HttpGet]
    public async Task<IActionResult> Manager(string document_number, string badge_number, string stage)
    {
      try
      {
        // Decode parameter dari Base64
        string documentNumber = document_number; // Sekarang berupa GUID string
        string badgeNumber = DecodeParameter<string>(badge_number);

        // Validasi badge number
        var employee = await _employeeService.GetEmployeeByLdapUserAsync(badgeNumber);
        if (employee == null || employee.PositionLvl != "MGR_LVL")
        {
          return View("AccessDenied");
        }

        // Dapatkan detail booking
        var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);
        if (booking == null)
        {
          return NotFound();
        }

        // Pastikan manager adalah dari departemen yang sama
        if (employee.Department != booking.Department)
        {
          return View("AccessDenied");
        }

        // Siapkan view model
        var viewModel = new ApprovalViewModel
        {
          BookingId = booking.Id,
          BadgeNumber = badgeNumber,
          EmployeeName = employee.Name,
          BookingDetails = booking
        };

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error displaying manager approval page");
        return View("Error");
      }
    }

    // [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Pic(string document_number, string badge_number, string stage)
    {
      try
      {
        // Decode parameter dari Base64
        string documentNumber = document_number; // Sekarang berupa GUID string
        string badgeNumber = DecodeParameter<string>(badge_number);

        // ✅ VALIDASI 1: Cek user exists
        var employee = await _employeeService.GetEmployeeByLdapUserAsync(badgeNumber);
        if (employee == null)
        {
          _logger.LogWarning("Employee not found for LDAP: {BadgeNumber}", badgeNumber);
          ViewBag.Message = $"User with LDAP '{badgeNumber}' not found.";
          return View("AccessDenied");
        }

        _logger.LogInformation("Employee found: {Name} ({LdapUser}) from {Department}",
            employee.Name, employee.LdapUser, employee.Department);

        // ✅ VALIDASI 2: Cek role PIC (ganti validasi department)
        var isPic = await _roleService.UserHasRoleAsync(badgeNumber, Roles.PIC);
        if (!isPic)
        {
          _logger.LogWarning("User {BadgeNumber} ({Name}) does not have PIC role", badgeNumber, employee.Name);
          ViewBag.Message = $"User '{employee.Name}' doesn't have PIC role permission.";
          return View("AccessDenied");
        }

        _logger.LogInformation("User {Name} has PIC role - access granted", employee.Name);

        // Dapatkan detail booking
        var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);
        if (booking == null)
        {
          _logger.LogWarning("Booking not found with DocumentNumber: {DocumentNumber}", documentNumber);
          return NotFound();
        }

        // Siapkan view model
        var viewModel = new ApprovalViewModel
        {
          BookingId = booking.Id,
          BadgeNumber = badgeNumber,
          EmployeeName = employee.Name,
          BookingDetails = booking
        };

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error displaying PIC approval page");
        return View("Error");
      }
    }

    // Controllers/ApprovalController.cs - Updated Manager methods

    [HttpPost]
    public async Task<IActionResult> ApproveByManager(int bookingId, string managerName)
    {
      try
      {
        // ✅ Cek status booking terlebih dahulu
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        TempData["DocumentNumber"] = booking.DocumentNumber;

        // ✅ Handle jika booking sudah di-approve oleh manager lain
        if (booking.Status == BookingStatus.ManagerApproved)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah disetujui oleh Manager {booking.ManagerName} pada {booking.ManagerApprovalTime?.ToString("dd/MM/yyyy HH:mm")}.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking sudah di-reject oleh manager
        if (booking.Status == BookingStatus.ManagerRejected)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah ditolak oleh Manager {booking.ManagerName} pada {booking.ManagerApprovalTime?.ToString("dd/MM/yyyy HH:mm")}.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking sudah diproses lebih lanjut (PIC approved/rejected)
        if (booking.Status == BookingStatus.PICApproved)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah selesai diproses. Disetujui oleh Manager {booking.ManagerName} dan PIC {booking.ApprovedByPIC}.";
          return RedirectToAction("Success");
        }

        if (booking.Status == BookingStatus.PICRejected)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah diproses. Disetujui oleh Manager {booking.ManagerName} namun ditolak oleh PIC.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking bukan dalam status yang tepat
        if (booking.Status != BookingStatus.PendingApproval)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} tidak dapat disetujui karena statusnya saat ini adalah {booking.Status}.";
          return RedirectToAction("Success");
        }

        var result = await _approvalService.ApproveByManagerAsync(bookingId, managerName);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking berhasil disetujui sebagai Manager.";
          var bookingRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = bookingRedirect.DocumentNumber;
          return RedirectToAction("Success");
        }
        else
        {
          TempData["SuccessMessage"] = "Booking tidak dapat disetujui karena mungkin sudah diproses oleh Manager lain.";
          var bookingRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = bookingRedirect.DocumentNumber;
          return RedirectToAction("Success");
        }
      }
      catch (KeyNotFoundException)
      {
        TempData["SuccessMessage"] = "Booking tidak ditemukan.";
        return RedirectToAction("Success");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error approving booking by manager");
        TempData["SuccessMessage"] = "Terjadi kesalahan sistem saat menyetujui booking.";
        return RedirectToAction("Success");
      }
    }

    [HttpPost]
    public async Task<IActionResult> RejectByManager(int bookingId, string managerName, string rejectReason)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(rejectReason))
        {
          TempData["ErrorMessage"] = "Alasan penolakan tidak boleh kosong.";
          // Redirect kembali ke halaman Manager dengan document number
          var bookingForRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          return RedirectToAction("Manager", new { document_number = bookingForRedirect.DocumentNumber });
        }

        // ✅ Cek status booking terlebih dahulu
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        TempData["DocumentNumber"] = booking.DocumentNumber;

        // ✅ Handle jika booking sudah di-approve oleh manager lain
        if (booking.Status == BookingStatus.ManagerApproved)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah disetujui oleh Manager {booking.ManagerName} pada {booking.ManagerApprovalTime?.ToString("dd/MM/yyyy HH:mm")}. Tidak dapat ditolak.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking sudah di-reject oleh manager
        if (booking.Status == BookingStatus.ManagerRejected)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah ditolak oleh Manager {booking.ManagerName} pada {booking.ManagerApprovalTime?.ToString("dd/MM/yyyy HH:mm")}.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking sudah diproses lebih lanjut
        if (booking.Status == BookingStatus.PICApproved || booking.Status == BookingStatus.PICRejected)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah diproses lebih lanjut dan tidak dapat ditolak di level Manager.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking bukan dalam status yang tepat
        if (booking.Status != BookingStatus.PendingApproval)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} tidak dapat ditolak karena statusnya saat ini adalah {booking.Status}.";
          return RedirectToAction("Success");
        }

        var result = await _approvalService.RejectByManagerAsync(bookingId, managerName, rejectReason);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking telah ditolak sebagai Manager.";
          var bookingRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = bookingRedirect.DocumentNumber;
          return RedirectToAction("Success");
        }
        else
        {
          TempData["SuccessMessage"] = "Booking tidak dapat ditolak karena mungkin sudah diproses oleh Manager lain.";
          return RedirectToAction("Success");
        }
      }
      catch (KeyNotFoundException)
      {
        TempData["SuccessMessage"] = "Booking tidak ditemukan.";
        return RedirectToAction("Success");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error rejecting booking by manager");
        TempData["SuccessMessage"] = "Terjadi kesalahan sistem saat menolak booking.";
        return RedirectToAction("Success");
      }
    }

    [HttpPost]
    public async Task<IActionResult> ApproveByPic(int bookingId, string picName, bool returnToDetails = false)
    {
      try
      {
        // ✅ Cek status booking terlebih dahulu
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        TempData["DocumentNumber"] = booking.DocumentNumber;

        // ✅ Handle jika booking sudah di-approve oleh PIC lain
        if (booking.Status == BookingStatus.PICApproved)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah disetujui oleh {booking.ApprovedByPIC} pada {booking.ApprovedAtByPIC?.ToString("dd/MM/yyyy HH:mm")}.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking sudah di-reject
        if (booking.Status == BookingStatus.PICRejected)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah ditolak sebelumnya.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking bukan dalam status yang tepat
        if (booking.Status != BookingStatus.ManagerApproved)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} tidak dapat disetujui karena statusnya saat ini adalah {booking.Status}.";
          return RedirectToAction("Success");
        }

        var result = await _approvalService.ApproveByPicAsync(bookingId, picName);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking berhasil disetujui.";
          var bookingRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = bookingRedirect.DocumentNumber;
          if (returnToDetails)
            return RedirectToAction("Details", "Booking", new { documentNumber = bookingRedirect.DocumentNumber });
          else
            return RedirectToAction("Success");
        }
        else
        {
          TempData["SuccessMessage"] = "Booking tidak dapat disetujui karena mungkin sudah diproses oleh PIC lain.";
          var bookingRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = bookingRedirect.DocumentNumber;
          if (returnToDetails)
            return RedirectToAction("Details", "Booking", new { documentNumber = bookingRedirect.DocumentNumber });
          else
            return RedirectToAction("Success");
        }
      }
      catch (KeyNotFoundException)
      {
        TempData["SuccessMessage"] = "Booking tidak ditemukan.";
        return RedirectToAction("Success");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error approving booking by PIC");
        TempData["SuccessMessage"] = "Terjadi kesalahan sistem saat menyetujui booking.";
        return RedirectToAction("Success");
      }
    }

    [HttpPost]
    public async Task<IActionResult> RejectByPic(int bookingId, string picName, string rejectReason, bool returnToDetails = false)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(rejectReason))
        {
          TempData["ErrorMessage"] = "Alasan penolakan tidak boleh kosong.";
          // Redirect kembali ke halaman PIC dengan document number
          var bookingForRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          return RedirectToAction("Pic", new { document_number = bookingForRedirect.DocumentNumber });
        }

        // ✅ Cek status booking terlebih dahulu
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        TempData["DocumentNumber"] = booking.DocumentNumber;

        // ✅ Handle jika booking sudah di-approve oleh PIC lain
        if (booking.Status == BookingStatus.PICApproved)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah disetujui oleh {booking.ApprovedByPIC} pada {booking.ApprovedAtByPIC?.ToString("dd/MM/yyyy HH:mm")}. Tidak dapat ditolak.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking sudah di-reject
        if (booking.Status == BookingStatus.PICRejected)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} sudah ditolak sebelumnya.";
          return RedirectToAction("Success");
        }

        // ✅ Handle jika booking bukan dalam status yang tepat
        if (booking.Status != BookingStatus.ManagerApproved)
        {
          TempData["SuccessMessage"] = $"Booking #{booking.BookingNumber} tidak dapat ditolak karena statusnya saat ini adalah {booking.Status}.";
          return RedirectToAction("Success");
        }

        var result = await _approvalService.RejectByPicAsync(bookingId, picName, rejectReason);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking telah ditolak.";
          var bookingRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = bookingRedirect.DocumentNumber;
          if (returnToDetails)
            return RedirectToAction("Details", "Booking", new { documentNumber = bookingRedirect.DocumentNumber });
          else
            return RedirectToAction("Success");
        }
        else
        {
          TempData["SuccessMessage"] = "Booking tidak dapat ditolak karena mungkin sudah diproses oleh PIC lain.";
          var bookingRedirect = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = bookingRedirect.DocumentNumber;
          if (returnToDetails)
            return RedirectToAction("Details", "Booking", new { documentNumber = bookingRedirect.DocumentNumber });
          else
            return RedirectToAction("Success");
        }
      }
      catch (KeyNotFoundException)
      {
        TempData["SuccessMessage"] = "Booking tidak ditemukan.";
        return RedirectToAction("Success");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error rejecting booking by PIC");
        TempData["SuccessMessage"] = "Terjadi kesalahan sistem saat menolak booking.";
        return RedirectToAction("Success");
      }
    }

    // Action untuk PIC menandai booking sebagai selesai
    [HttpPost]
    public async Task<IActionResult> MarkAsDone(int bookingId, string picName)
    {
      try
      {
        var result = await _approvalService.MarkAsDoneAsync(bookingId, picName);
        if (result)
        {
          TempData["BookingSuccessMessage"] = "Booking telah ditandai sebagai selesai.";
          var booking = await _bookingService.GetBookingByIdAsync(bookingId);
          TempData["DocumentNumber"] = booking.DocumentNumber;
          return RedirectToAction("Details", "Booking", new { documentNumber = booking.DocumentNumber });
        }
        else
        {
          TempData["ErrorMessage"] = "Terjadi kesalahan saat menandai booking sebagai selesai.";
          return RedirectToAction("Error");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error marking booking as done");
        TempData["ErrorMessage"] = "Terjadi kesalahan saat menandai booking sebagai selesai.";
        return RedirectToAction("Error");
      }
    }

    // Halaman sukses
    public IActionResult Success()
    {
      return View();
    }

    // Halaman error
    public IActionResult Error()
    {
      return View();
    }

    // Helper method untuk mendecode parameter Base64
    private T DecodeParameter<T>(string encodedValue)
    {
      if (string.IsNullOrEmpty(encodedValue))
        throw new ArgumentException("Parameter encoded value tidak boleh kosong");

      byte[] bytes = Convert.FromBase64String(encodedValue);
      string decodedString = Encoding.UTF8.GetString(bytes);

      if (typeof(T) == typeof(int))
      {
        if (int.TryParse(decodedString, out int result))
          return (T)(object)result;
        throw new FormatException("Tidak dapat mengubah nilai ke tipe int");
      }
      else if (typeof(T) == typeof(string))
      {
        return (T)(object)decodedString;
      }
      else
      {
        throw new NotSupportedException($"Tipe {typeof(T)} tidak didukung");
      }
    }
  }
}
