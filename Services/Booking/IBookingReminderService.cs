// Services/Booking/IBookingReminderService.cs
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Services
{
  public interface IBookingReminderService
  {
    Task SendDailyBookingRemindersAsync();
    Task<int> GetPendingRemindersCountAsync();
    Task<IEnumerable<Booking>> GetBookingsNeedingReminderAsync(DateTime targetDate);
  }
}
