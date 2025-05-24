// Services/CraneManagement/CraneService.cs (Updated)
using Hangfire;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.CraneManagement;
// using AspnetCoreMvcFull.Events;

namespace AspnetCoreMvcFull.Services
{
  public class CraneService : ICraneService
  {
    private readonly AppDbContext _context;
    private readonly ILogger<CraneService> _logger;
    private readonly IFileStorageService _fileStorage;
    private readonly IEmailService _emailService;
    private readonly IEmployeeService _employeeService;
    private const string ContainerName = "cranes";

    public CraneService(AppDbContext context, ILogger<CraneService> logger, IFileStorageService fileStorage, IEmailService emailService, IEmployeeService employeeService)

    {
      _context = context;
      _logger = logger;
      _fileStorage = fileStorage;
      _emailService = emailService;
      _employeeService = employeeService;
    }

    public async Task<IEnumerable<CraneViewModel>> GetAllCranesAsync()
    {
      var cranes = await _context.Cranes
          .OrderBy(c => c.Code)
          .ToListAsync();

      return cranes.Select(c => new CraneViewModel
      {
        Id = c.Id,
        Code = c.Code,
        Capacity = c.Capacity,
        Status = c.Status,
        ImagePath = c.ImagePath,
        Ownership = c.Ownership
      }).ToList();
    }

    public async Task<CraneDetailViewModel> GetCraneByIdAsync(int id)
    {
      var crane = await _context.Cranes
          .Include(c => c.Breakdowns.OrderByDescending(ul => ul.UrgentStartTime))
          .FirstOrDefaultAsync(c => c.Id == id);

      if (crane == null)
      {
        throw new KeyNotFoundException($"Crane with ID {id} not found");
      }

      var craneDetailViewModel = new CraneDetailViewModel
      {
        Id = crane.Id,
        Code = crane.Code,
        Capacity = crane.Capacity,
        Status = crane.Status,
        ImagePath = crane.ImagePath,
        Ownership = crane.Ownership,
        Breakdowns = crane.Breakdowns?.Select(ul => new BreakdownViewModel
        {
          Id = ul.Id,
          CraneId = ul.CraneId ?? 0,
          UrgentStartTime = ul.UrgentStartTime,
          UrgentEndTime = ul.UrgentEndTime,
          ActualUrgentEndTime = ul.ActualUrgentEndTime,
          HangfireJobId = ul.HangfireJobId,
          Reasons = ul.Reasons
        }).ToList() ?? new List<BreakdownViewModel>()
      };

      return craneDetailViewModel;
    }

    public async Task<IEnumerable<BreakdownViewModel>> GetCraneBreakdownsAsync(int id)
    {
      if (!await CraneExistsAsync(id))
      {
        throw new KeyNotFoundException($"Crane with ID {id} not found");
      }

      var breakdowns = await _context.Breakdowns
          .Where(ul => ul.CraneId == id)
          .OrderByDescending(ul => ul.UrgentStartTime)
          .ToListAsync();

      return breakdowns.Select(ul => new BreakdownViewModel
      {
        Id = ul.Id,
        CraneId = ul.CraneId ?? 0,
        UrgentStartTime = ul.UrgentStartTime,
        UrgentEndTime = ul.UrgentEndTime,
        ActualUrgentEndTime = ul.ActualUrgentEndTime,
        HangfireJobId = ul.HangfireJobId,
        Reasons = ul.Reasons
      }).ToList();
    }

    public async Task<CraneViewModel> CreateCraneAsync(CraneCreateViewModel craneViewModel)
    {
      var crane = new Crane
      {
        Code = craneViewModel.Code,
        Capacity = craneViewModel.Capacity,
        Status = craneViewModel.Status ?? CraneStatus.Available,
        Ownership = craneViewModel.Ownership
      };

      // Upload image if provided
      if (craneViewModel.Image != null)
      {
        crane.ImagePath = await _fileStorage.SaveFileAsync(craneViewModel.Image, ContainerName);
      }

      _context.Cranes.Add(crane);
      await _context.SaveChangesAsync();

      return new CraneViewModel
      {
        Id = crane.Id,
        Code = crane.Code,
        Capacity = crane.Capacity,
        Status = crane.Status,
        ImagePath = crane.ImagePath,
        Ownership = crane.Ownership
      };
    }

    public async Task UpdateCraneAsync(int id, CraneUpdateWithBreakdownViewModel updateViewModel)
    {
      var existingCrane = await _context.Cranes
          .Include(c => c.Breakdowns.OrderByDescending(u => u.UrgentStartTime).Take(1))
          .FirstOrDefaultAsync(c => c.Id == id);

      if (existingCrane == null)
      {
        throw new KeyNotFoundException($"Crane with ID {id} not found");
      }

      // Update image if provided
      if (updateViewModel.Crane.Image != null && updateViewModel.Crane.Image.Length > 0)
      {
        // Delete old image if exists
        if (!string.IsNullOrEmpty(existingCrane.ImagePath))
        {
          await _fileStorage.DeleteFileAsync(existingCrane.ImagePath, ContainerName);
        }

        // Upload new image
        existingCrane.ImagePath = await _fileStorage.SaveFileAsync(updateViewModel.Crane.Image, ContainerName);
      }

      // Update data other than status (Code, Capacity, Ownership)
      existingCrane.Code = updateViewModel.Crane.Code;
      existingCrane.Capacity = updateViewModel.Crane.Capacity;
      existingCrane.Ownership = updateViewModel.Crane.Ownership;

      // If crane status is changed to Maintenance
      if (updateViewModel.Crane.Status.HasValue && updateViewModel.Crane.Status != existingCrane.Status &&
          updateViewModel.Crane.Status == CraneStatus.Maintenance)
      {
        existingCrane.Status = CraneStatus.Maintenance;

        // Validate if BreakdownViewModel is provided
        if (updateViewModel.Breakdown != null)
        {
          // Validate required fields
          if (string.IsNullOrEmpty(updateViewModel.Breakdown.Reasons))
          {
            throw new ArgumentException("Reasons is required for maintenance status");
          }

          // Create new Breakdown
          var breakdown = new Breakdown
          {
            CraneId = existingCrane.Id,
            // Tambahkan data historis crane
            CraneCode = existingCrane.Code,
            CraneCapacity = existingCrane.Capacity,
            UrgentStartTime = updateViewModel.Breakdown.UrgentStartTime,
            UrgentEndTime = updateViewModel.Breakdown.UrgentEndTime,
            Reasons = updateViewModel.Breakdown.Reasons
          };

          // Add breakdown to database
          _context.Breakdowns.Add(breakdown);

          // Save changes to get Breakdown ID
          await _context.SaveChangesAsync();

          // ✅ NEW: Send notifications to affected bookings
          await SendBreakdownNotificationsAsync(existingCrane.Id, breakdown);

          // Calculate the delay time
          TimeSpan delayTime = breakdown.UrgentEndTime - DateTime.Now;

          // Schedule a BackgroundJob to change crane status to Available after UrgentEndTime
          string jobId = BackgroundJob.Schedule(() => ChangeCraneStatusToAvailableAsync(existingCrane.Id), delayTime);

          // Save JobId to Breakdown
          breakdown.HangfireJobId = jobId;
          await _context.SaveChangesAsync();
        }
        else
        {
          throw new ArgumentException("Breakdown data is required when changing status to Maintenance");
        }
      }
      // If crane status is changed from Maintenance to Available manually
      else if (updateViewModel.Crane.Status.HasValue &&
              existingCrane.Status == CraneStatus.Maintenance &&
              updateViewModel.Crane.Status == CraneStatus.Available)
      {
        existingCrane.Status = CraneStatus.Available;

        // If there's an active Breakdown, update ActualUrgentEndTime
        var latestBreakdown = existingCrane.Breakdowns.FirstOrDefault();
        if (latestBreakdown != null && latestBreakdown.ActualUrgentEndTime == null)
        {
          latestBreakdown.ActualUrgentEndTime = DateTime.Now;

          // Cancel scheduled job if there's a JobId
          if (!string.IsNullOrEmpty(latestBreakdown.HangfireJobId))
          {
            try
            {
              BackgroundJob.Delete(latestBreakdown.HangfireJobId);
              _logger.LogInformation("Cancelled Hangfire job {JobId} for crane {CraneId}",
                  latestBreakdown.HangfireJobId, existingCrane.Id);
            }
            catch (Exception ex)
            {
              _logger.LogWarning(ex, "Failed to delete Hangfire job {JobId} for crane {CraneId}",
                  latestBreakdown.HangfireJobId, existingCrane.Id);
            }

            // Clear job ID
            latestBreakdown.HangfireJobId = null;
          }
        }
      }
      else
      {
        // If not changing status to Maintenance, just update crane data
        if (updateViewModel.Crane.Status.HasValue)
        {
          existingCrane.Status = updateViewModel.Crane.Status.Value;
        }
      }

      _context.Entry(existingCrane).State = EntityState.Modified;
      await _context.SaveChangesAsync();
    }

    // CraneService.cs - Add this new method
    private async Task SendBreakdownNotificationsAsync(int craneId, Breakdown breakdown)
    {
      try
      {
        _logger.LogInformation("Sending breakdown notifications for crane {CraneId}", craneId);

        // Get crane info for historical data check
        var crane = await _context.Cranes.FindAsync(craneId);
        string craneCode = crane?.Code ?? "";

        // Find all active bookings that might be affected by this breakdown
        var affectedBookings = await _context.Bookings
            .Include(b => b.BookingShifts)
            .Where(b =>
                // Booking untuk crane yang sama (by ID atau Code)
                (b.CraneId == craneId || b.CraneCode == craneCode) &&

                // Booking yang masih aktif (belum selesai/dibatalkan)
                b.Status == BookingStatus.PICApproved &&

                // Booking yang overlap dengan periode breakdown
                b.StartDate <= breakdown.UrgentEndTime.Date &&
                b.EndDate >= breakdown.UrgentStartTime.Date)
            .ToListAsync();

        _logger.LogInformation("Found {Count} potentially affected bookings", affectedBookings.Count);

        // Send email to each affected booking owner
        foreach (var booking in affectedBookings)
        {
          try
          {
            if (!string.IsNullOrEmpty(booking.LdapUser))
            {
              var user = await _employeeService.GetEmployeeByLdapUserAsync(booking.LdapUser);
              if (user != null && !string.IsNullOrEmpty(user.Email))
              {
                await _emailService.SendBookingAffectedByBreakdownEmailAsync(booking, user.Email, breakdown);

                _logger.LogInformation(
                    "Breakdown notification sent to {UserEmail} for booking {BookingNumber}",
                    user.Email, booking.BookingNumber);
              }
              else
              {
                _logger.LogWarning(
                    "User email not found for LDAP {LdapUser} in booking {BookingNumber}",
                    booking.LdapUser, booking.BookingNumber);
              }
            }
            else
            {
              _logger.LogWarning("LDAP user not available for booking {BookingNumber}", booking.BookingNumber);
            }
          }
          catch (Exception emailEx)
          {
            _logger.LogError(emailEx,
                "Failed to send breakdown notification for booking {BookingNumber}",
                booking.BookingNumber);
            // Continue with other bookings even if one fails
          }
        }

        _logger.LogInformation("Completed sending breakdown notifications for crane {CraneId}", craneId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error sending breakdown notifications for crane {CraneId}", craneId);
        // Don't throw - this shouldn't fail the main breakdown operation
      }
    }

    // Method to update only the image
    public async Task<bool> UpdateCraneImageAsync(int id, IFormFile image)
    {
      var crane = await _context.Cranes.FindAsync(id);
      if (crane == null)
      {
        return false;
      }

      // Delete old image if exists
      if (!string.IsNullOrEmpty(crane.ImagePath))
      {
        await _fileStorage.DeleteFileAsync(crane.ImagePath, ContainerName);
      }

      // Upload new image
      crane.ImagePath = await _fileStorage.SaveFileAsync(image, ContainerName);

      _context.Entry(crane).State = EntityState.Modified;
      await _context.SaveChangesAsync();

      return true;
    }

    // Method to remove crane image
    public async Task RemoveCraneImageAsync(int id)
    {
      var crane = await _context.Cranes.FindAsync(id);
      if (crane == null)
      {
        throw new KeyNotFoundException($"Crane with ID {id} not found");
      }

      crane.ImagePath = null;
      _context.Entry(crane).State = EntityState.Modified;
      await _context.SaveChangesAsync();
    }

    public async Task DeleteCraneAsync(int id)
    {
      var crane = await _context.Cranes
          .Include(c => c.Breakdowns)
          .FirstOrDefaultAsync(c => c.Id == id);

      if (crane == null)
      {
        throw new KeyNotFoundException($"Crane with ID {id} not found");
      }

      // Delete image if exists
      if (!string.IsNullOrEmpty(crane.ImagePath))
      {
        await _fileStorage.DeleteFileAsync(crane.ImagePath, ContainerName);
      }

      // Periksa apakah ada booking yang menggunakan crane ini
      var relatedBookings = await _context.Bookings
          .Where(b => b.CraneId == id)
          .ToListAsync();

      // Update semua booking terkait dengan data historis crane
      foreach (var booking in relatedBookings)
      {
        booking.CraneCode = crane.Code;
        booking.CraneCapacity = crane.Capacity;
        booking.CraneId = null;
      }

      // Periksa apakah ada maintenance schedule yang menggunakan crane ini
      var relatedSchedules = await _context.MaintenanceSchedules
          .Where(ms => ms.CraneId == id)
          .ToListAsync();

      // Update semua maintenance schedule terkait dengan data historis crane
      foreach (var schedule in relatedSchedules)
      {
        schedule.CraneCode = crane.Code;
        schedule.CraneCapacity = crane.Capacity;
        schedule.CraneId = null;
      }

      // Delete all related Breakdowns
      var relatedLogs = await _context.Breakdowns.Where(ul => ul.CraneId == id).ToListAsync();

      // Update semua breakdowns terkait dengan data historis crane
      foreach (var log in relatedLogs)
      {
        // Simpan data historis crane
        log.CraneCode = crane.Code;
        log.CraneCapacity = crane.Capacity;
        log.CraneId = null;
      }

      // Cancel all related Hangfire jobs
      foreach (var log in relatedLogs.Where(l => !string.IsNullOrEmpty(l.HangfireJobId)))
      {
        try
        {
          BackgroundJob.Delete(log.HangfireJobId);
          _logger.LogInformation("Deleted Hangfire job {JobId} for crane {CraneId}", log.HangfireJobId, id);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Failed to delete Hangfire job {JobId}", log.HangfireJobId);
        }
      }

      // Simpan perubahan pada breakdowns
      await _context.SaveChangesAsync();

      // _context.Breakdowns.RemoveRange(relatedLogs);
      _context.Cranes.Remove(crane);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Crane {CraneId} deleted and {BookingCount} related bookings and {ScheduleCount} maintenance schedules updated with historical data",
        id, relatedBookings.Count, relatedSchedules.Count);
    }

    public async Task ChangeCraneStatusToAvailableAsync(int craneId)
    {
      _logger.LogInformation("Executing scheduled job to change crane {CraneId} status to Available", craneId);

      var crane = await _context.Cranes
          .Include(c => c.Breakdowns.OrderByDescending(u => u.UrgentStartTime).Take(1))
          .FirstOrDefaultAsync(c => c.Id == craneId);

      if (crane != null && crane.Status == CraneStatus.Maintenance)
      {
        var latestLog = crane.Breakdowns.FirstOrDefault();

        // If not marked as manually completed
        if (latestLog != null && latestLog.ActualUrgentEndTime == null)
        {
          crane.Status = CraneStatus.Available;
          latestLog.ActualUrgentEndTime = DateTime.Now;

          await _context.SaveChangesAsync();
          _logger.LogInformation("Crane {CraneId} status automatically changed to Available via Hangfire job", craneId);
        }
        else
        {
          _logger.LogInformation("Crane {CraneId} already has ActualUrgentEndTime set, no action needed", craneId);
        }
      }
      else
      {
        _logger.LogInformation("Crane {CraneId} is not in Maintenance status or does not exist, no action needed", craneId);
      }
    }

    public async Task<IEnumerable<BreakdownHistoryViewModel>> GetAllBreakdownsAsync()
    {
      var breakdowns = await _context.Breakdowns
          .Include(b => b.Crane)
          .OrderByDescending(b => b.UrgentStartTime)
          .ToListAsync();

      return breakdowns.Select(b => new BreakdownHistoryViewModel
      {
        Id = b.Id,
        CraneId = b.CraneId ?? 0, // Null-coalescing operator untuk mengatasi nullable CraneId
        // Prioritaskan data yang ada di Crane, gunakan data historis jika Crane null
        CraneCode = b.CraneId.HasValue && b.Crane != null ? b.Crane.Code : b.CraneCode ?? "Unknown",
        CraneCapacity = b.CraneId.HasValue && b.Crane != null ? b.Crane.Capacity : b.CraneCapacity ?? 0,
        UrgentStartTime = b.UrgentStartTime,
        UrgentEndTime = b.UrgentEndTime,
        ActualUrgentEndTime = b.ActualUrgentEndTime,
        Reasons = b.Reasons
      }).ToList();
    }

    public async Task<bool> CraneExistsAsync(int id)
    {
      return await _context.Cranes.AnyAsync(e => e.Id == id);
    }
  }
}
