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
    // private readonly IEventPublisher _eventPublisher;
    private readonly IFileStorageService _fileStorage;
    private const string ContainerName = "cranes";

    // public CraneService(AppDbContext context, ILogger<CraneService> logger, IEventPublisher eventPublisher, IFileStorageService fileStorage)
    public CraneService(AppDbContext context, ILogger<CraneService> logger, IFileStorageService fileStorage)

    {
      _context = context;
      _logger = logger;
      // _eventPublisher = eventPublisher;
      _fileStorage = fileStorage;
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
          CraneId = ul.CraneId,
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
        CraneId = ul.CraneId,
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
            UrgentStartTime = updateViewModel.Breakdown.UrgentStartTime,
            UrgentEndTime = updateViewModel.Breakdown.UrgentEndTime,
            Reasons = updateViewModel.Breakdown.Reasons
          };

          // Add breakdown to database
          _context.Breakdowns.Add(breakdown);

          // Save changes to get Breakdown ID
          await _context.SaveChangesAsync();

          // Calculate the delay time
          TimeSpan delayTime = breakdown.UrgentEndTime - DateTime.Now;

          // Schedule a BackgroundJob to change crane status to Available after UrgentEndTime
          string jobId = BackgroundJob.Schedule(() => ChangeCraneStatusToAvailableAsync(existingCrane.Id), delayTime);

          // Save JobId to Breakdown
          breakdown.HangfireJobId = jobId;
          await _context.SaveChangesAsync();

          // // Publish event for relocation
          // await _eventPublisher.PublishAsync(new CraneMaintenanceEvent
          // {
          //   CraneId = existingCrane.Id,
          //   MaintenanceStartTime = breakdown.UrgentStartTime,
          //   MaintenanceEndTime = breakdown.UrgentEndTime,
          //   Reason = breakdown.Reasons
          // });
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
      var crane = await _context.Cranes.FindAsync(id);
      if (crane == null)
      {
        throw new KeyNotFoundException($"Crane with ID {id} not found");
      }

      // Delete image if exists
      if (!string.IsNullOrEmpty(crane.ImagePath))
      {
        await _fileStorage.DeleteFileAsync(crane.ImagePath, ContainerName);
      }

      // Delete all related Breakdowns
      var relatedLogs = await _context.Breakdowns.Where(ul => ul.CraneId == id).ToListAsync();

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

      _context.Breakdowns.RemoveRange(relatedLogs);
      _context.Cranes.Remove(crane);
      await _context.SaveChangesAsync();
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

          // // Publish event for checking any necessary relocations after maintenance
          // await _eventPublisher.PublishAsync(new CraneMaintenanceEvent
          // {
          //   CraneId = craneId,
          //   MaintenanceStartTime = latestLog.UrgentStartTime,
          //   MaintenanceEndTime = DateTime.Now,
          //   Reason = latestLog.Reasons
          // });
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
        CraneId = b.CraneId,
        CraneCode = b.Crane?.Code ?? "Unknown",
        CraneCapacity = b.Crane?.Capacity ?? 0,
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
