// Services/Email/IEmailService.cs
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Services
{
  public interface IEmailService
  {
    Task SendEmailAsync(string to, string subject, string body);
    Task SendBookingSubmittedEmailAsync(Booking booking, string userEmail);
    Task SendManagerApprovalRequestEmailAsync(Booking booking, string managerEmail, string managerName, string badgeNumber);
    Task SendBookingManagerApprovedEmailAsync(Booking booking, string userEmail);
    Task SendPicApprovalRequestEmailAsync(Booking booking, string picEmail, string picName, string badgeNumber);
    Task SendBookingApprovedEmailAsync(Booking booking, string userEmail);
    Task SendBookingRejectedEmailAsync(Booking booking, string userEmail, string rejectorName, string rejectReason);
    Task SendBookingCancelledEmailAsync(Booking booking, string userEmail, string cancelledBy, string cancelReason);
    Task SendBookingRevisedEmailAsync(Booking booking, string receiverEmail);
  }
}
