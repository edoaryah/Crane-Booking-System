using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels;
using System.Text;

namespace AspnetCoreMvcFull.Controllers
{
  public class ApprovalController : Controller
  {
    private readonly IBookingService _bookingService;
    private readonly IBookingApprovalService _approvalService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(
        IBookingService bookingService,
        IBookingApprovalService approvalService,
        IEmployeeService employeeService,
        ILogger<ApprovalController> logger)
    {
      _bookingService = bookingService;
      _approvalService = approvalService;
      _employeeService = employeeService;
      _logger = logger;
    }

    // Halaman approval untuk Manager
    [HttpGet]
    public async Task<IActionResult> Manager(string document_number, string badge_number, string stage)
    {
      try
      {
        // Decode parameter dari Base64
        int bookingId = int.Parse(document_number); // Atau DecodeParameter<int> jika perlu
        string badgeNumber = DecodeParameter<string>(badge_number);

        // Validasi badge number
        var employee = await _employeeService.GetEmployeeByLdapUserAsync(badgeNumber);
        if (employee == null || employee.PositionLvl != "MGR_LVL")
        {
          return View("AccessDenied");
        }

        // Dapatkan detail booking
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
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
          BookingId = bookingId,
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

    // Halaman approval untuk PIC Crane
    [HttpGet]
    public async Task<IActionResult> Pic(string document_number, string badge_number, string stage)
    {
      try
      {
        // Decode parameter dari Base64
        int bookingId = int.Parse(document_number); ;
        string badgeNumber = DecodeParameter<string>(badge_number);

        // Validasi badge number
        var employee = await _employeeService.GetEmployeeByLdapUserAsync(badgeNumber);
        if (employee == null || employee.PositionLvl != "SUPV_LVL" || employee.Department != "Stores & Inventory Control")
        {
          return View("AccessDenied");
        }

        // Dapatkan detail booking
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
          return NotFound();
        }

        // Siapkan view model
        var viewModel = new ApprovalViewModel
        {
          BookingId = bookingId,
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

    // Action untuk Manager menyetujui booking
    [HttpPost]
    public async Task<IActionResult> ApproveByManager(int bookingId, string managerName)
    {
      try
      {
        var result = await _approvalService.ApproveByManagerAsync(bookingId, managerName);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking berhasil disetujui.";
          return RedirectToAction("Success");
        }
        else
        {
          TempData["ErrorMessage"] = "Terjadi kesalahan saat menyetujui booking.";
          return RedirectToAction("Error");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error approving booking by manager");
        TempData["ErrorMessage"] = "Terjadi kesalahan saat menyetujui booking.";
        return RedirectToAction("Error");
      }
    }

    // Action untuk Manager menolak booking
    [HttpPost]
    public async Task<IActionResult> RejectByManager(int bookingId, string managerName, string rejectReason)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(rejectReason))
        {
          TempData["ErrorMessage"] = "Alasan penolakan tidak boleh kosong.";
          return RedirectToAction("Manager", new { id = bookingId });
        }

        var result = await _approvalService.RejectByManagerAsync(bookingId, managerName, rejectReason);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking telah ditolak.";
          return RedirectToAction("Success");
        }
        else
        {
          TempData["ErrorMessage"] = "Terjadi kesalahan saat menolak booking.";
          return RedirectToAction("Error");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error rejecting booking by manager");
        TempData["ErrorMessage"] = "Terjadi kesalahan saat menolak booking.";
        return RedirectToAction("Error");
      }
    }

    // Action untuk PIC menyetujui booking
    [HttpPost]
    public async Task<IActionResult> ApproveByPic(int bookingId, string picName)
    {
      try
      {
        var result = await _approvalService.ApproveByPicAsync(bookingId, picName);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking berhasil disetujui.";
          return RedirectToAction("Success");
        }
        else
        {
          TempData["ErrorMessage"] = "Terjadi kesalahan saat menyetujui booking.";
          return RedirectToAction("Error");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error approving booking by PIC");
        TempData["ErrorMessage"] = "Terjadi kesalahan saat menyetujui booking.";
        return RedirectToAction("Error");
      }
    }

    // Action untuk PIC menolak booking
    [HttpPost]
    public async Task<IActionResult> RejectByPic(int bookingId, string picName, string rejectReason)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(rejectReason))
        {
          TempData["ErrorMessage"] = "Alasan penolakan tidak boleh kosong.";
          return RedirectToAction("Pic", new { id = bookingId });
        }

        var result = await _approvalService.RejectByPicAsync(bookingId, picName, rejectReason);
        if (result)
        {
          TempData["SuccessMessage"] = "Booking telah ditolak.";
          return RedirectToAction("Success");
        }
        else
        {
          TempData["ErrorMessage"] = "Terjadi kesalahan saat menolak booking.";
          return RedirectToAction("Error");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error rejecting booking by PIC");
        TempData["ErrorMessage"] = "Terjadi kesalahan saat menolak booking.";
        return RedirectToAction("Error");
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
          TempData["SuccessMessage"] = "Booking telah ditandai sebagai selesai.";
          return RedirectToAction("Success");
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
