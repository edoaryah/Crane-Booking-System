// Services/Booking/BookingApprovalService.cs - RELIABLE FIX
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services
{
  public class BookingApprovalService : IBookingApprovalService
  {
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<BookingApprovalService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory; // ✅ GANTI KE IServiceScopeFactory

    public BookingApprovalService(
        AppDbContext context,
        IEmailService emailService,
        IEmployeeService employeeService,
        ILogger<BookingApprovalService> logger,
        IServiceScopeFactory serviceScopeFactory) // ✅ GANTI KE IServiceScopeFactory
    {
      _context = context;
      _emailService = emailService;
      _employeeService = employeeService;
      _logger = logger;
      _serviceScopeFactory = serviceScopeFactory; // ✅ GANTI KE IServiceScopeFactory
    }

    public async Task<bool> ApproveByManagerAsync(int bookingId, string managerName)
    {
      try
      {
        var booking = await _context.Bookings
            .Include(b => b.Crane)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        if (booking.Status != BookingStatus.PendingApproval)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status PendingApproval", bookingId);
          return false;
        }

        booking.Status = BookingStatus.ManagerApproved;
        booking.ManagerName = managerName;
        booking.ManagerApprovalTime = DateTime.Now;

        await _context.SaveChangesAsync();

        // ✅ RELIABLE FIX: Immediate background task with better error handling
        _logger.LogInformation("Starting background email task for manager approval of booking {BookingNumber}", booking.BookingNumber);

        _ = Task.Run(async () =>
        {
          try
          {
            _logger.LogInformation("Background email task started for booking {BookingNumber}", booking.BookingNumber);

            using var scope = _serviceScopeFactory.CreateScope();
            var scopedEmployeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            _logger.LogInformation("Scoped services created for booking {BookingNumber}", booking.BookingNumber);

            // Kirim notifikasi email ke user
            var user = await scopedEmployeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
              await scopedEmailService.SendBookingManagerApprovedEmailAsync(booking, user.Email);
              _logger.LogInformation("✅ Manager approval notification sent to user {UserEmail} for booking {BookingNumber}",
                  user.Email, booking.BookingNumber);
            }
            else
            {
              _logger.LogWarning("❌ User email not found for LDAP {LdapUser} in booking {BookingNumber}",
                  booking.LdapUser, booking.BookingNumber);
            }

            // Kirim notifikasi email ke semua PIC crane
            _logger.LogInformation("Getting PIC list for booking {BookingNumber}", booking.BookingNumber);
            var picCranes = await scopedEmployeeService.GetPicCraneAsync();
            _logger.LogInformation("Found {PicCount} PIC users for booking {BookingNumber}", picCranes.Count(), booking.BookingNumber);

            foreach (var pic in picCranes)
            {
              _logger.LogInformation("Processing PIC: {PicName} ({PicLdap}) - Email: {PicEmail}",
                  pic.Name, pic.LdapUser, pic.Email);

              if (!string.IsNullOrEmpty(pic.Email) && !string.IsNullOrEmpty(pic.LdapUser))
              {
                await scopedEmailService.SendPicApprovalRequestEmailAsync(
                    booking,
                    pic.Email,
                    pic.Name,
                    pic.LdapUser);

                _logger.LogInformation("✅ PIC approval request sent to {PicName} ({PicEmail}) for booking {BookingNumber}",
                    pic.Name, pic.Email, booking.BookingNumber);
              }
              else
              {
                _logger.LogWarning("❌ PIC {PicName} missing email or LDAP - Email: {PicEmail}, LDAP: {PicLdap}",
                    pic.Name, pic.Email, pic.LdapUser);
              }
            }

            _logger.LogInformation("✅ Background email task completed for booking {BookingNumber}", booking.BookingNumber);
          }
          catch (Exception emailEx)
          {
            _logger.LogError(emailEx, "❌ Error in background email task for booking {BookingNumber}", booking.BookingNumber);
          }
        });

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saat menyetujui booking dengan ID {BookingId} oleh manager", bookingId);
        throw;
      }
    }

    public async Task<bool> RejectByManagerAsync(int bookingId, string managerName, string rejectReason)
    {
      try
      {
        var booking = await _context.Bookings
            .Include(b => b.Crane)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        if (booking.Status != BookingStatus.PendingApproval)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status PendingApproval", bookingId);
          return false;
        }

        booking.Status = BookingStatus.ManagerRejected;
        booking.ManagerName = managerName;
        booking.ManagerApprovalTime = DateTime.Now;
        booking.ManagerRejectReason = rejectReason;

        await _context.SaveChangesAsync();

        // ✅ RELIABLE FIX: Background task with logging
        _logger.LogInformation("Starting background email task for manager rejection of booking {BookingNumber}", booking.BookingNumber);

        _ = Task.Run(async () =>
        {
          try
          {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedEmployeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var user = await scopedEmployeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
              await scopedEmailService.SendBookingRejectedEmailAsync(
                  booking,
                  user.Email,
                  managerName,
                  rejectReason);

              _logger.LogInformation("✅ Manager rejection notification sent to user {UserEmail} for booking {BookingNumber}",
                  user.Email, booking.BookingNumber);
            }
          }
          catch (Exception emailEx)
          {
            _logger.LogError(emailEx, "❌ Error sending rejection email for booking {BookingNumber}", booking.BookingNumber);
          }
        });

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saat menolak booking dengan ID {BookingId} oleh manager", bookingId);
        throw;
      }
    }

    public async Task<bool> ApproveByPicAsync(int bookingId, string picName)
    {
      try
      {
        var booking = await _context.Bookings
            .Include(b => b.Crane)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        if (booking.Status != BookingStatus.ManagerApproved)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status ManagerApproved", bookingId);
          return false;
        }

        booking.Status = BookingStatus.PICApproved;
        booking.ApprovedByPIC = picName;
        booking.ApprovedAtByPIC = DateTime.Now;

        await _context.SaveChangesAsync();

        // ✅ RELIABLE FIX: Background task with logging
        _logger.LogInformation("Starting background email task for PIC approval of booking {BookingNumber}", booking.BookingNumber);

        _ = Task.Run(async () =>
        {
          try
          {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedEmployeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var user = await scopedEmployeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
              await scopedEmailService.SendBookingApprovedEmailAsync(booking, user.Email);
              _logger.LogInformation("✅ PIC approval notification sent to user {UserEmail} for booking {BookingNumber}",
                  user.Email, booking.BookingNumber);
            }
          }
          catch (Exception emailEx)
          {
            _logger.LogError(emailEx, "❌ Error sending PIC approval email for booking {BookingNumber}", booking.BookingNumber);
          }
        });

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saat menyetujui booking dengan ID {BookingId} oleh PIC", bookingId);
        throw;
      }
    }

    public async Task<bool> RejectByPicAsync(int bookingId, string picName, string rejectReason)
    {
      try
      {
        var booking = await _context.Bookings
            .Include(b => b.Crane)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        if (booking.Status != BookingStatus.ManagerApproved)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status ManagerApproved", bookingId);
          return false;
        }

        booking.Status = BookingStatus.PICRejected;
        booking.PICRejectReason = rejectReason;

        await _context.SaveChangesAsync();

        // ✅ RELIABLE FIX: Background task with logging
        _logger.LogInformation("Starting background email task for PIC rejection of booking {BookingNumber}", booking.BookingNumber);

        _ = Task.Run(async () =>
        {
          try
          {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedEmployeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var user = await scopedEmployeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
              await scopedEmailService.SendBookingRejectedEmailAsync(
                  booking,
                  user.Email,
                  picName,
                  rejectReason);

              _logger.LogInformation("✅ PIC rejection notification sent to user {UserEmail} for booking {BookingNumber}",
                  user.Email, booking.BookingNumber);
            }
          }
          catch (Exception emailEx)
          {
            _logger.LogError(emailEx, "❌ Error sending PIC rejection email for booking {BookingNumber}", booking.BookingNumber);
          }
        });

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saat menolak booking dengan ID {BookingId} oleh PIC", bookingId);
        throw;
      }
    }

    public async Task<bool> MarkAsDoneAsync(int bookingId, string picName)
    {
      try
      {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        if (booking.Status != BookingStatus.PICApproved)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status PICApproved", bookingId);
          return false;
        }

        booking.Status = BookingStatus.Done;
        booking.DoneByPIC = picName;
        booking.DoneAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saat menandai booking dengan ID {BookingId} sebagai selesai", bookingId);
        throw;
      }
    }

    public async Task<bool> CancelBookingAsync(int bookingId, BookingCancelledBy cancelledBy, string cancelledByName, string cancelReason)
    {
      try
      {
        var booking = await _context.Bookings
            .Include(b => b.Crane)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        if (booking.Status == BookingStatus.Done)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dapat dibatalkan karena sudah selesai", bookingId);
          return false;
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} sudah dibatalkan sebelumnya", bookingId);
          return false;
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledBy = cancelledBy;
        booking.CancelledByName = cancelledByName;
        booking.CancelledAt = DateTime.Now;
        booking.CancelledReason = cancelReason;

        await _context.SaveChangesAsync();

        // ✅ RELIABLE FIX: Background task with logging
        _logger.LogInformation("Starting background email task for booking cancellation {BookingNumber}", booking.BookingNumber);

        _ = Task.Run(async () =>
        {
          try
          {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedEmployeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            if (!string.IsNullOrEmpty(booking.LdapUser))
            {
              var user = await scopedEmployeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
              if (user != null && !string.IsNullOrEmpty(user.Email))
              {
                await scopedEmailService.SendBookingCancelledEmailAsync(booking, user.Email, cancelledByName, cancelReason);
                _logger.LogInformation("✅ Email pembatalan booking dikirim ke user {UserEmail} untuk booking {BookingNumber}",
                    user.Email, booking.BookingNumber);
              }
              else
              {
                _logger.LogWarning("❌ Email user tidak ditemukan untuk LDAP {LdapUser} pada booking {BookingNumber}",
                    booking.LdapUser, booking.BookingNumber);
              }
            }
            else
            {
              _logger.LogWarning("❌ LDAP user tidak tersedia untuk booking {BookingNumber}", booking.BookingNumber);
            }
          }
          catch (Exception emailEx)
          {
            _logger.LogError(emailEx, "❌ Error sending cancellation email for booking {BookingNumber}", booking.BookingNumber);
          }
        });

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saat membatalkan booking dengan ID {BookingId}", bookingId);
        throw;
      }
    }

    public async Task<bool> ReviseRejectedBookingAsync(int bookingId, string revisedByName)
    {
      try
      {
        var booking = await _context.Bookings
            .Include(b => b.Crane)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        if (booking.Status != BookingStatus.ManagerRejected && booking.Status != BookingStatus.PICRejected)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status yang dapat direvisi. Current status: {Status}",
              bookingId, booking.Status);
          return false;
        }

        var originalStatus = booking.Status;

        booking.Status = BookingStatus.PendingApproval;

        if (originalStatus == BookingStatus.ManagerRejected)
        {
          booking.ManagerName = null;
          booking.ManagerApprovalTime = null;
          booking.ManagerRejectReason = null;
          _logger.LogInformation("Reset manager rejection fields for booking {BookingId}", bookingId);
        }
        else if (originalStatus == BookingStatus.PICRejected)
        {
          booking.ManagerName = null;
          booking.ManagerApprovalTime = null;
          booking.ManagerRejectReason = null;
          booking.PICRejectReason = null;
          _logger.LogInformation("Reset all approval fields for booking {BookingId} - revision from PIC rejection", bookingId);
        }

        await _context.SaveChangesAsync();

        // ✅ RELIABLE FIX: Background task with logging
        _logger.LogInformation("Starting background email task for booking revision {BookingNumber}", booking.BookingNumber);

        _ = Task.Run(async () =>
        {
          try
          {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedEmployeeService = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
            var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var manager = await scopedEmployeeService.GetManagerByDepartmentAsync(booking.Department);
            if (manager != null && !string.IsNullOrEmpty(manager.Email) && !string.IsNullOrEmpty(manager.LdapUser))
            {
              await scopedEmailService.SendManagerApprovalRequestEmailAsync(
                  booking,
                  manager.Email,
                  manager.Name,
                  manager.LdapUser);

              _logger.LogInformation("✅ Manager approval email sent for revised booking {BookingId} (original status: {OriginalStatus})",
                  bookingId, originalStatus);
            }
            else
            {
              _logger.LogWarning("❌ Manager not found for department {Department} for booking {BookingId}",
                  booking.Department, bookingId);
            }

            var user = await scopedEmployeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
              await scopedEmailService.SendBookingRevisedEmailAsync(booking, user.Email);
              _logger.LogInformation("✅ Revision notification sent to user for booking {BookingId}", bookingId);
            }
          }
          catch (Exception emailEx)
          {
            _logger.LogError(emailEx, "❌ Error sending emails for revised booking {BookingId}", bookingId);
          }
        });

        _logger.LogInformation("Booking {BookingId} successfully revised by {RevisedBy}. Approval process restarted from manager.",
            bookingId, revisedByName);

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saat merevisi booking dengan ID {BookingId}", bookingId);
        throw;
      }
    }
  }
}
