// Services/Booking/BookingApprovalService.cs
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

    public BookingApprovalService(
        AppDbContext context,
        IEmailService emailService,
        IEmployeeService employeeService,
        ILogger<BookingApprovalService> logger)
    {
      _context = context;
      _emailService = emailService;
      _employeeService = employeeService;
      _logger = logger;
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

        // Memastikan booking dalam status yang benar
        if (booking.Status != BookingStatus.PendingApproval)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status PendingApproval", bookingId);
          return false;
        }

        // Update status booking menjadi ManagerApproved
        booking.Status = BookingStatus.ManagerApproved;
        booking.ManagerName = managerName;
        booking.ManagerApprovalTime = DateTime.Now;

        await _context.SaveChangesAsync();

        // Kirim notifikasi email ke user
        var user = await _employeeService.GetEmployeeByLdapUserAsync(booking.Name);
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
          await _emailService.SendBookingManagerApprovedEmailAsync(booking, user.Email);
        }

        // Kirim notifikasi email ke semua PIC crane
        var picCranes = await _employeeService.GetPicCraneAsync();
        foreach (var pic in picCranes)
        {
          if (!string.IsNullOrEmpty(pic.Email) && !string.IsNullOrEmpty(pic.LdapUser))
          {
            await _emailService.SendPicApprovalRequestEmailAsync(
                booking,
                pic.Email,
                pic.Name,
                pic.LdapUser);
          }
        }

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

        // Memastikan booking dalam status yang benar
        if (booking.Status != BookingStatus.PendingApproval)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status PendingApproval", bookingId);
          return false;
        }

        // Update status booking menjadi ManagerRejected
        booking.Status = BookingStatus.ManagerRejected;
        booking.ManagerName = managerName;
        booking.ManagerApprovalTime = DateTime.Now;
        booking.ManagerRejectReason = rejectReason;

        await _context.SaveChangesAsync();

        // Kirim notifikasi email ke user
        var user = await _employeeService.GetEmployeeByLdapUserAsync(booking.Name);
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
          await _emailService.SendBookingRejectedEmailAsync(
              booking,
              user.Email,
              managerName,
              rejectReason);
        }

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

        // Memastikan booking dalam status yang benar
        if (booking.Status != BookingStatus.ManagerApproved)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status ManagerApproved", bookingId);
          return false;
        }

        // Update status booking menjadi PICApproved
        booking.Status = BookingStatus.PICApproved;
        booking.ApprovedByPIC = picName;
        booking.ApprovedAtByPIC = DateTime.Now;

        await _context.SaveChangesAsync();

        // Kirim notifikasi email ke user
        var user = await _employeeService.GetEmployeeByLdapUserAsync(booking.Name);
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
          await _emailService.SendBookingApprovedEmailAsync(booking, user.Email);
        }

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

        // Memastikan booking dalam status yang benar
        if (booking.Status != BookingStatus.ManagerApproved)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status ManagerApproved", bookingId);
          return false;
        }

        // Update status booking menjadi PICRejected
        booking.Status = BookingStatus.PICRejected;
        booking.PICRejectReason = rejectReason;

        await _context.SaveChangesAsync();

        // Kirim notifikasi email ke user
        var user = await _employeeService.GetEmployeeByLdapUserAsync(booking.Name);
        if (user != null && !string.IsNullOrEmpty(user.Email))
        {
          await _emailService.SendBookingRejectedEmailAsync(
              booking,
              user.Email,
              picName,
              rejectReason);
        }

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

        // Memastikan booking dalam status yang benar
        if (booking.Status != BookingStatus.PICApproved)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status PICApproved", bookingId);
          return false;
        }

        // Update status booking menjadi Done
        booking.Status = BookingStatus.Done;
        booking.DoneByPIC = picName;
        booking.DoneAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // Tidak perlu email notifikasi untuk pembaruan status menjadi Done

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
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        // Memastikan booking belum selesai
        if (booking.Status == BookingStatus.Done)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dapat dibatalkan karena sudah selesai", bookingId);
          return false;
        }

        // Update status booking menjadi Cancelled
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledBy = cancelledBy;
        booking.CancelledByName = cancelledByName;
        booking.CancelledAt = DateTime.Now;
        booking.CancelledReason = cancelReason;

        await _context.SaveChangesAsync();

        // Kirim notifikasi email ke semua pihak terkait
        // TODO: Implementasi email notifikasi pembatalan

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
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak ditemukan", bookingId);
          return false;
        }

        // Memastikan booking dalam status yang bisa direvisi
        if (booking.Status != BookingStatus.ManagerRejected && booking.Status != BookingStatus.PICRejected)
        {
          _logger.LogWarning("Booking dengan ID {BookingId} tidak dalam status yang dapat direvisi", bookingId);
          return false;
        }

        // Reset status booking menjadi PendingApproval
        booking.Status = BookingStatus.PendingApproval;
        booking.RevisionCount++;
        booking.LastModifiedAt = DateTime.Now;
        booking.LastModifiedBy = revisedByName;

        // Reset approval fields jika perlu
        if (booking.Status == BookingStatus.PICRejected)
        {
          // Jika ditolak oleh PIC, kita perlu reset status approval PIC
          booking.PICRejectReason = null;
          // Tetap mempertahankan approval Manager
        }
        else
        {
          // Jika ditolak oleh Manager, reset semua status approval
          booking.ManagerName = null;
          booking.ManagerApprovalTime = null;
          booking.ManagerRejectReason = null;
        }

        await _context.SaveChangesAsync();

        // Kirim notifikasi email ke pihak terkait
        // TODO: Implementasi email notifikasi revisi

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
