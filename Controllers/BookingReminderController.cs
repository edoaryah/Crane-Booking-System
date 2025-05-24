// Controllers/Admin/BookingReminderController.cs
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Filters;

namespace AspnetCoreMvcFull.Controllers.Admin
{
  [ApiController]
  [Route("api/[controller]")]
  public class BookingReminderController : ControllerBase
  {
    private readonly IBookingReminderService _reminderService;
    private readonly ILogger<BookingReminderController> _logger;

    public BookingReminderController(
        IBookingReminderService reminderService,
        ILogger<BookingReminderController> logger)
    {
      _reminderService = reminderService;
      _logger = logger;
    }

    [HttpPost("send-daily-reminders")]
    public async Task<IActionResult> SendDailyReminders()
    {
      try
      {
        _logger.LogInformation("Manual trigger: Daily reminders requested");
        await _reminderService.SendDailyBookingRemindersAsync();
        return Ok(new
        {
          success = true,
          message = "Daily reminders sent successfully",
          timestamp = DateTime.Now
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in manual daily reminders trigger");
        return StatusCode(500, new
        {
          success = false,
          message = "Error sending daily reminders: " + ex.Message
        });
      }
    }

    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount()
    {
      try
      {
        var count = await _reminderService.GetPendingRemindersCountAsync();
        return Ok(new
        {
          success = true,
          pendingReminders = count,
          checkDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd")
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting pending reminders count");
        return StatusCode(500, new
        {
          success = false,
          message = "Error getting pending count: " + ex.Message
        });
      }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetReminderStatus()
    {
      try
      {
        var tomorrow = DateTime.Today.AddDays(1);
        var bookings = await _reminderService.GetBookingsNeedingReminderAsync(tomorrow);

        return Ok(new
        {
          success = true,
          targetDate = tomorrow.ToString("yyyy-MM-dd"),
          totalBookings = bookings.Count(),
          bookings = bookings.Select(b => new
          {
            id = b.Id,
            bookingNumber = b.BookingNumber,
            userName = b.Name,
            craneCode = b.CraneCode,
            location = b.Location,
            submitTime = b.SubmitTime,
            reminderSent = b.ReminderEmailSent
          })
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting reminder status");
        return StatusCode(500, new
        {
          success = false,
          message = "Error getting status: " + ex.Message
        });
      }
    }
  }
}
