// Services/Booking/BookingReminderService.cs
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Services
{
  public class BookingReminderService : IBookingReminderService
  {
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<BookingReminderService> _logger;

    public BookingReminderService(
        AppDbContext context,
        IEmailService emailService,
        IEmployeeService employeeService,
        ILogger<BookingReminderService> logger)
    {
      _context = context;
      _emailService = emailService;
      _employeeService = employeeService;
      _logger = logger;
    }

    public async Task SendDailyBookingRemindersAsync()
    {
      try
      {
        var tomorrow = DateTime.Today.AddDays(1);
        _logger.LogInformation("Starting daily booking reminders for date: {Date}", tomorrow.ToString("yyyy-MM-dd"));

        var bookingsNeedReminder = await GetBookingsNeedingReminderAsync(tomorrow);

        _logger.LogInformation("Found {Count} bookings needing reminders", bookingsNeedReminder.Count());

        var successCount = 0;
        var failureCount = 0;

        foreach (var booking in bookingsNeedReminder)
        {
          try
          {
            await SendReminderForBookingAsync(booking);
            successCount++;

            _logger.LogInformation(
                "Reminder sent successfully for booking {BookingNumber} to user {LdapUser}",
                booking.BookingNumber, booking.LdapUser);
          }
          catch (Exception ex)
          {
            failureCount++;
            _logger.LogError(ex,
                "Failed to send reminder for booking {BookingNumber} to user {LdapUser}",
                booking.BookingNumber, booking.LdapUser);
          }
        }

        _logger.LogInformation(
            "Daily booking reminders completed. Success: {SuccessCount}, Failures: {FailureCount}",
            successCount, failureCount);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in SendDailyBookingRemindersAsync");
        throw;
      }
    }

    public async Task<IEnumerable<Booking>> GetBookingsNeedingReminderAsync(DateTime targetDate)
    {
      var cutoffTime = DateTime.Now.AddHours(-24);

      return await _context.Bookings
          .Include(b => b.BookingShifts)
          .Where(b =>
              // ✅ Booking starts tomorrow
              b.StartDate.Date == targetDate.Date &&

              // ✅ Status is PICApproved
              b.Status == BookingStatus.PICApproved &&

              // ✅ Reminder not sent yet
              !b.ReminderEmailSent &&

              // ✅ Skip last-minute bookings (created H-1 or H-0)
              b.SubmitTime <= cutoffTime &&

              // ✅ Has valid LDAP user
              !string.IsNullOrEmpty(b.LdapUser))
          .ToListAsync();
    }

    public async Task<int> GetPendingRemindersCountAsync()
    {
      var tomorrow = DateTime.Today.AddDays(1);
      var bookings = await GetBookingsNeedingReminderAsync(tomorrow);
      return bookings.Count();
    }

    private async Task SendReminderForBookingAsync(Booking booking)
    {
      // Get user email
      var user = await _employeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
      if (user == null || string.IsNullOrEmpty(user.Email))
      {
        throw new InvalidOperationException($"User email not found for LDAP: {booking.LdapUser}");
      }

      // Send reminder email
      await _emailService.SendBookingReminderEmailAsync(booking, user.Email);

      // Update tracking flags
      booking.ReminderEmailSent = true;
      booking.ReminderEmailSentAt = DateTime.Now;
      booking.ReminderEmailSentTo = user.Email;

      await _context.SaveChangesAsync();
    }
  }
}
