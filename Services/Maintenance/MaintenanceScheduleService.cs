using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.Common;
// using AspnetCoreMvcFull.Events;
using AspnetCoreMvcFull.ViewModels.MaintenanceManagement;

namespace AspnetCoreMvcFull.Services
{
  public class MaintenanceScheduleService : IMaintenanceScheduleService
  {
    private readonly AppDbContext _context;
    private readonly ICraneService _craneService;
    private readonly IShiftDefinitionService _shiftDefinitionService;
    private readonly IScheduleConflictService _scheduleConflictService;
    // private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<MaintenanceScheduleService> _logger;

    public MaintenanceScheduleService(
        AppDbContext context,
        ICraneService craneService,
        IShiftDefinitionService shiftDefinitionService,
        IScheduleConflictService scheduleConflictService,
        // IEventPublisher eventPublisher,
        ILogger<MaintenanceScheduleService> logger)
    {
      _context = context;
      _craneService = craneService;
      _shiftDefinitionService = shiftDefinitionService;
      _scheduleConflictService = scheduleConflictService;
      // _eventPublisher = eventPublisher;
      _logger = logger;
    }

    public async Task<IEnumerable<MaintenanceScheduleViewModel>> GetAllMaintenanceSchedulesAsync()
    {
      var schedules = await _context.MaintenanceSchedules
          .Include(m => m.Crane)
          .OrderByDescending(m => m.CreatedAt)
          .ToListAsync();

      return schedules.Select(m => new MaintenanceScheduleViewModel
      {
        Id = m.Id,
        DocumentNumber = m.DocumentNumber,
        CraneId = m.CraneId ?? 0,
        // Prioritaskan data historis yang disimpan jika CraneId null
        CraneCode = m.CraneId.HasValue ? m.Crane?.Code : m.CraneCode,
        Title = m.Title,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Description = m.Description,
        CreatedAt = m.CreatedAt,
        CreatedBy = m.CreatedBy
      }).ToList();
    }

    public async Task<MaintenanceScheduleDetailViewModel> GetMaintenanceScheduleByIdAsync(int id)
    {
      var schedule = await _context.MaintenanceSchedules
          .Include(m => m.Crane)
          .Include(m => m.MaintenanceScheduleShifts)
            .ThenInclude(ms => ms.ShiftDefinition)
          .FirstOrDefaultAsync(m => m.Id == id);

      if (schedule == null)
      {
        throw new KeyNotFoundException($"Maintenance schedule with ID {id} not found");
      }

      return new MaintenanceScheduleDetailViewModel
      {
        Id = schedule.Id,
        DocumentNumber = schedule.DocumentNumber,
        CraneId = schedule.CraneId ?? 0,
        // Prioritaskan data historis yang disimpan jika CraneId null
        CraneCode = schedule.CraneId.HasValue ? schedule.Crane?.Code : schedule.CraneCode,
        Title = schedule.Title,
        StartDate = schedule.StartDate,
        EndDate = schedule.EndDate,
        Description = schedule.Description,
        CreatedAt = schedule.CreatedAt,
        CreatedBy = schedule.CreatedBy,
        UpdatedAt = schedule.UpdatedAt,
        UpdatedBy = schedule.UpdatedBy,
        Shifts = schedule.MaintenanceScheduleShifts.Select(s => new MaintenanceScheduleShiftViewModel
        {
          Id = s.Id,
          Date = s.Date,
          // Gunakan nilai default 0 jika ShiftDefinitionId null
          ShiftDefinitionId = s.ShiftDefinitionId ?? 0,
          // Prioritaskan data historis yang disimpan
          ShiftName = s.ShiftName ?? s.ShiftDefinition?.Name ?? "Unknown Shift",
          // Prioritaskan data historis yang disimpan, gunakan nilai default jika keduanya tidak ada
          StartTime = s.ShiftStartTime != default ? s.ShiftStartTime : s.ShiftDefinition?.StartTime,
          EndTime = s.ShiftEndTime != default ? s.ShiftEndTime : s.ShiftDefinition?.EndTime
        }).ToList()
      };
    }

    public async Task<MaintenanceScheduleDetailViewModel> GetMaintenanceScheduleByDocumentNumberAsync(string documentNumber)
    {
      var schedule = await _context.MaintenanceSchedules
          .Include(m => m.Crane)
          .Include(m => m.MaintenanceScheduleShifts)
            .ThenInclude(ms => ms.ShiftDefinition)
          .FirstOrDefaultAsync(m => m.DocumentNumber == documentNumber);

      if (schedule == null)
      {
        throw new KeyNotFoundException($"Maintenance schedule with document number {documentNumber} not found");
      }

      return new MaintenanceScheduleDetailViewModel
      {
        Id = schedule.Id,
        DocumentNumber = schedule.DocumentNumber,
        CraneId = schedule.CraneId ?? 0,
        // Prioritaskan data historis yang disimpan jika CraneId null
        CraneCode = schedule.CraneId.HasValue ? schedule.Crane?.Code : schedule.CraneCode,
        Title = schedule.Title,
        StartDate = schedule.StartDate,
        EndDate = schedule.EndDate,
        Description = schedule.Description,
        CreatedAt = schedule.CreatedAt,
        CreatedBy = schedule.CreatedBy,
        UpdatedAt = schedule.UpdatedAt,
        UpdatedBy = schedule.UpdatedBy,
        Shifts = schedule.MaintenanceScheduleShifts.Select(s => new MaintenanceScheduleShiftViewModel
        {
          Id = s.Id,
          Date = s.Date,
          // Gunakan nilai default 0 jika ShiftDefinitionId null
          ShiftDefinitionId = s.ShiftDefinitionId ?? 0,
          // Prioritaskan data historis yang disimpan
          ShiftName = s.ShiftName ?? s.ShiftDefinition?.Name ?? "Unknown Shift",
          // Prioritaskan data historis yang disimpan, gunakan nilai default jika keduanya tidak ada
          StartTime = s.ShiftStartTime != default ? s.ShiftStartTime : s.ShiftDefinition?.StartTime,
          EndTime = s.ShiftEndTime != default ? s.ShiftEndTime : s.ShiftDefinition?.EndTime
        }).ToList()
      };
    }

    public async Task<IEnumerable<MaintenanceScheduleViewModel>> GetMaintenanceSchedulesByCraneIdAsync(int craneId)
    {
      // Dapatkan Crane terlebih dahulu untuk mendapatkan kode
      var crane = await _context.Cranes.FindAsync(craneId);
      if (crane == null)
      {
        throw new KeyNotFoundException($"Crane with ID {craneId} not found");
      }

      var schedules = await _context.MaintenanceSchedules
          .Include(m => m.Crane)
          .Where(m => m.CraneId == craneId || (m.CraneId == null && m.CraneCode == crane.Code))
          .OrderByDescending(m => m.CreatedAt)
          .ToListAsync();

      return schedules.Select(m => new MaintenanceScheduleViewModel
      {
        Id = m.Id,
        DocumentNumber = m.DocumentNumber,
        CraneId = m.CraneId ?? 0,
        // Prioritaskan data historis yang disimpan jika CraneId null
        CraneCode = m.CraneId.HasValue ? m.Crane?.Code : m.CraneCode,
        Title = m.Title,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Description = m.Description,
        CreatedAt = m.CreatedAt,
        CreatedBy = m.CreatedBy
      }).ToList();
    }

    public async Task<MaintenanceScheduleDetailViewModel> CreateMaintenanceScheduleAsync(MaintenanceScheduleCreateViewModel maintenanceViewModel)
    {
      try
      {
        _logger.LogInformation("Creating maintenance schedule for crane {CraneId}", maintenanceViewModel.CraneId);

        // Validate crane exists
        var crane = await _context.Cranes.FindAsync(maintenanceViewModel.CraneId);
        if (crane == null)
        {
          throw new KeyNotFoundException($"Crane with ID {maintenanceViewModel.CraneId} not found");
        }

        // Get crane code for conflict checking
        string craneCode = crane.Code;

        // Gunakan tanggal lokal tanpa konversi UTC
        var startDate = maintenanceViewModel.StartDate.Date;
        var endDate = maintenanceViewModel.EndDate.Date;

        // Validate date range
        if (startDate > endDate)
        {
          throw new ArgumentException("Start date must be before or equal to end date");
        }

        // Validate shift selections
        if (maintenanceViewModel.ShiftSelections == null || !maintenanceViewModel.ShiftSelections.Any())
        {
          throw new ArgumentException("At least one shift selection is required");
        }

        // Check if all dates in the range have shift selections
        var dateRange = Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(d => startDate.AddDays(d))
            .ToList();

        var selectedDates = maintenanceViewModel.ShiftSelections
            .Select(s => s.Date.Date)
            .ToList();

        if (!dateRange.All(d => selectedDates.Contains(d)))
        {
          throw new ArgumentException("All dates in the range must have shift selections");
        }

        // Validate each shift selection has at least one shift selected and check for conflicts
        foreach (var selection in maintenanceViewModel.ShiftSelections)
        {
          if (selection.SelectedShiftIds == null || !selection.SelectedShiftIds.Any())
          {
            throw new ArgumentException($"At least one shift must be selected for date {selection.Date.ToShortDateString()}");
          }

          // Gunakan tanggal lokal untuk pengecekan konflik
          var dateLocal = selection.Date.Date;

          // Check for scheduling conflicts for each selected shift
          foreach (var shiftId in selection.SelectedShiftIds)
          {
            // Verify the shift definition exists
            var shiftDefinition = await _context.ShiftDefinitions.FindAsync(shiftId);
            if (shiftDefinition == null)
            {
              throw new KeyNotFoundException($"Shift definition with ID {shiftId} not found");
            }

            // PERBAIKAN: Gunakan validasi berbasis waktu untuk memeriksa konflik dengan maintenance lain
            bool hasMaintenanceConflict = await _scheduleConflictService.IsMaintenanceConflictByTimeAsync(
                maintenanceViewModel.CraneId,
                dateLocal,
                shiftDefinition.StartTime,
                shiftDefinition.EndTime,
                null,
                craneCode);  // Teruskan craneCode

            if (hasMaintenanceConflict)
            {
              throw new InvalidOperationException($"Scheduling conflict with existing maintenance detected for date {dateLocal.ToShortDateString()} and shift {shiftDefinition.Name}");
            }

            // Periksa konflik dengan booking yang ada
            bool hasBookingConflict = await _scheduleConflictService.IsBookingConflictByTimeAsync(
                maintenanceViewModel.CraneId,
                dateLocal,
                shiftDefinition.StartTime,
                shiftDefinition.EndTime,
                null,
                craneCode);  // Teruskan craneCode

            if (hasBookingConflict)
            {
              throw new InvalidOperationException($"Scheduling conflict with existing booking detected for date {dateLocal.ToShortDateString()} and shift {shiftDefinition.Name}");
            }
          }
        }

        // Create maintenance schedule with a new unique document number
        var schedule = new MaintenanceSchedule
        {
          DocumentNumber = Guid.NewGuid().ToString(),
          CraneId = maintenanceViewModel.CraneId,
          // Simpan data historis crane
          CraneCode = crane.Code,
          CraneCapacity = crane.Capacity,
          Title = maintenanceViewModel.Title,
          StartDate = startDate,
          EndDate = endDate,
          Description = maintenanceViewModel.Description,
          CreatedAt = DateTime.Now,
          CreatedBy = maintenanceViewModel.CreatedBy ?? "system"
        };

        _context.MaintenanceSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Create shift selections with historical data
        foreach (var selection in maintenanceViewModel.ShiftSelections)
        {
          var dateLocal = selection.Date.Date;

          foreach (var shiftId in selection.SelectedShiftIds)
          {
            // Dapatkan informasi shift saat ini
            var shiftDefinition = await _context.ShiftDefinitions.FindAsync(shiftId);
            if (shiftDefinition == null)
            {
              throw new KeyNotFoundException($"Shift definition with ID {shiftId} not found");
            }

            var scheduleShift = new MaintenanceScheduleShift
            {
              MaintenanceScheduleId = schedule.Id,
              Date = dateLocal,
              ShiftDefinitionId = shiftId,
              // Simpan juga data historis shift
              ShiftName = shiftDefinition.Name,
              ShiftStartTime = shiftDefinition.StartTime,
              ShiftEndTime = shiftDefinition.EndTime
            };

            _context.MaintenanceScheduleShifts.Add(scheduleShift);
          }
        }

        await _context.SaveChangesAsync();

        // Publish event untuk relokasi booking yang terdampak
        // await _eventPublisher.PublishAsync(new CraneMaintenanceEvent
        // {
        //   CraneId = schedule.CraneId,
        //   MaintenanceStartTime = schedule.StartDate,
        //   MaintenanceEndTime = schedule.EndDate,
        //   Reason = $"Scheduled Maintenance: {schedule.Title}"
        // });

        // Return the created maintenance schedule with details
        return await GetMaintenanceScheduleByIdAsync(schedule.Id);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating maintenance schedule: {Message}", ex.Message);
        throw;
      }
    }

    public async Task<MaintenanceScheduleDetailViewModel> UpdateMaintenanceScheduleAsync(int id, MaintenanceScheduleUpdateViewModel maintenanceViewModel, string updatedBy)
    {
      try
      {
        _logger.LogInformation("Updating maintenance schedule ID: {Id}", id);

        var schedule = await _context.MaintenanceSchedules
            .Include(m => m.MaintenanceScheduleShifts)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (schedule == null)
        {
          throw new KeyNotFoundException($"Maintenance schedule with ID {id} not found");
        }

        schedule.UpdatedAt = DateTime.Now;
        schedule.UpdatedBy = updatedBy;

        // Validate crane exists if changing crane
        var crane = await _context.Cranes.FindAsync(maintenanceViewModel.CraneId);
        if (crane == null)
        {
          throw new KeyNotFoundException($"Crane with ID {maintenanceViewModel.CraneId} not found");
        }

        // Get crane code for conflict checking
        string craneCode = crane.Code;

        // Jika CraneId berubah, update data historis crane
        if (schedule.CraneId != maintenanceViewModel.CraneId)
        {
          schedule.CraneId = maintenanceViewModel.CraneId;
          schedule.CraneCode = crane.Code;
          schedule.CraneCapacity = crane.Capacity;
        }

        // Gunakan tanggal lokal tanpa konversi UTC
        var startDate = maintenanceViewModel.StartDate.Date;
        var endDate = maintenanceViewModel.EndDate.Date;

        // Validate date range
        if (startDate > endDate)
        {
          throw new ArgumentException("Start date must be before or equal to end date");
        }

        // Validate shift selections
        if (maintenanceViewModel.ShiftSelections == null || !maintenanceViewModel.ShiftSelections.Any())
        {
          throw new ArgumentException("At least one shift selection is required");
        }

        // Check if all dates in the range have shift selections
        var dateRange = Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(d => startDate.AddDays(d))
            .ToList();

        var selectedDates = maintenanceViewModel.ShiftSelections
            .Select(s => s.Date.Date)
            .ToList();

        if (!dateRange.All(d => selectedDates.Contains(d)))
        {
          throw new ArgumentException("All dates in the range must have shift selections");
        }

        // Validate each shift selection has at least one shift selected
        foreach (var selection in maintenanceViewModel.ShiftSelections)
        {
          if (selection.SelectedShiftIds == null || !selection.SelectedShiftIds.Any())
          {
            throw new ArgumentException($"At least one shift must be selected for date {selection.Date.ToShortDateString()}");
          }

          // Gunakan tanggal lokal untuk pengecekan konflik
          var dateLocal = selection.Date.Date;

          // Check for scheduling conflicts for each selected shift
          foreach (var shiftId in selection.SelectedShiftIds)
          {
            // Verify the shift definition exists
            var shiftDefinition = await _context.ShiftDefinitions.FindAsync(shiftId);
            if (shiftDefinition == null)
            {
              throw new KeyNotFoundException($"Shift definition with ID {shiftId} not found");
            }

            // PERBAIKAN: Gunakan validasi berbasis waktu untuk memeriksa konflik dengan maintenance lain
            bool hasMaintenanceConflict = await _scheduleConflictService.IsMaintenanceConflictByTimeAsync(
                maintenanceViewModel.CraneId,
                dateLocal,
                shiftDefinition.StartTime,
                shiftDefinition.EndTime,
                id,  // Exclude current maintenance schedule
                craneCode);  // Teruskan craneCode

            if (hasMaintenanceConflict)
            {
              throw new InvalidOperationException($"Scheduling conflict with existing maintenance schedule detected for date {dateLocal.ToShortDateString()} and shift {shiftDefinition.Name}");
            }

            // Cek apakah ada konflik dengan booking yang ada
            bool hasBookingConflict = await _scheduleConflictService.IsBookingConflictByTimeAsync(
                maintenanceViewModel.CraneId,
                dateLocal,
                shiftDefinition.StartTime,
                shiftDefinition.EndTime,
                null,
                craneCode);  // Teruskan craneCode

            if (hasBookingConflict)
            {
              throw new InvalidOperationException($"Scheduling conflict with existing booking detected for date {dateLocal.ToShortDateString()} and shift {shiftDefinition.Name}");
            }
          }
        }

        // Capture previous values for event
        var previousCraneId = schedule.CraneId;
        var previousStartDate = schedule.StartDate;
        var previousEndDate = schedule.EndDate;

        // Update maintenance schedule
        schedule.Title = maintenanceViewModel.Title;
        schedule.StartDate = startDate;
        schedule.EndDate = endDate;
        schedule.Description = maintenanceViewModel.Description;
        // DocumentNumber and CreatedAt/CreatedBy are not changed

        // Remove existing shift selections
        _context.MaintenanceScheduleShifts.RemoveRange(schedule.MaintenanceScheduleShifts);

        // Create new shift selections with historical data
        foreach (var selection in maintenanceViewModel.ShiftSelections)
        {
          var dateLocal = selection.Date.Date;

          foreach (var shiftId in selection.SelectedShiftIds)
          {
            // Dapatkan informasi shift saat ini
            var shiftDefinition = await _context.ShiftDefinitions.FindAsync(shiftId);
            if (shiftDefinition == null)
            {
              throw new KeyNotFoundException($"Shift definition with ID {shiftId} not found");
            }

            var scheduleShift = new MaintenanceScheduleShift
            {
              MaintenanceScheduleId = schedule.Id,
              Date = dateLocal,
              ShiftDefinitionId = shiftId,
              // Simpan juga data historis shift
              ShiftName = shiftDefinition.Name,
              ShiftStartTime = shiftDefinition.StartTime,
              ShiftEndTime = shiftDefinition.EndTime
            };

            _context.MaintenanceScheduleShifts.Add(scheduleShift);
          }
        }

        await _context.SaveChangesAsync();

        // Jika ada perubahan pada crane, tanggal, atau shift yang mempengaruhi booking,
        // publish event untuk relokasi booking yang terdampak
        if (previousCraneId != schedule.CraneId ||
            previousStartDate != schedule.StartDate ||
            previousEndDate != schedule.EndDate)
        {
          // Jika crane berubah, handle relokasi untuk crane lama
          if (previousCraneId != schedule.CraneId)
          {
            // Tidak perlu merelokasi booking pada crane lama
          }

          // // Publish event untuk relokasi booking yang terdampak pada crane baru
          // await _eventPublisher.PublishAsync(new CraneMaintenanceEvent
          // {
          //   CraneId = schedule.CraneId,
          //   MaintenanceStartTime = schedule.StartDate,
          //   MaintenanceEndTime = schedule.EndDate,
          //   Reason = $"Updated Scheduled Maintenance: {schedule.Title}"
          // });
        }

        // Return the updated maintenance schedule with details
        return await GetMaintenanceScheduleByIdAsync(schedule.Id);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating maintenance schedule: {Message}", ex.Message);
        throw;
      }
    }

    public async Task DeleteMaintenanceScheduleAsync(int id)
    {
      try
      {
        _logger.LogInformation("Deleting maintenance schedule ID: {Id}", id);

        var schedule = await _context.MaintenanceSchedules
            .Include(m => m.MaintenanceScheduleShifts)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (schedule == null)
        {
          throw new KeyNotFoundException($"Maintenance schedule with ID {id} not found");
        }

        // Remove all associated shifts
        _context.MaintenanceScheduleShifts.RemoveRange(schedule.MaintenanceScheduleShifts);

        // Remove the maintenance schedule
        _context.MaintenanceSchedules.Remove(schedule);

        await _context.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting maintenance schedule: {Message}", ex.Message);
        throw;
      }
    }

    public async Task<PagedResult<MaintenanceScheduleViewModel>> GetPagedMaintenanceSchedulesAsync(
    MaintenanceHistoryFilterRequest request,
    string? currentUser = null,
    List<string>? userRoles = null)
    {
      try
      {
        // Start with all records
        var query = _context.MaintenanceSchedules
            .Include(m => m.Crane)
            .AsQueryable();

        // ✅ ROLE-BASED DATA SCOPING
        if (userRoles != null && !string.IsNullOrEmpty(currentUser))
        {
          var isAdmin = userRoles.Contains("admin", StringComparer.OrdinalIgnoreCase);
          var isPic = userRoles.Contains("pic", StringComparer.OrdinalIgnoreCase);
          var isMsd = userRoles.Contains("msd", StringComparer.OrdinalIgnoreCase);

          if (!isAdmin && !isPic && !isMsd)
          {
            // ✅ SIMPLE: Selain Admin/PIC/MSD tidak ada akses
            query = query.Where(m => false);
            _logger.LogWarning("User {CurrentUser} with roles [{Roles}] has no access to maintenance data",
                             currentUser, string.Join(", ", userRoles));
          }
          else
          {
            // ✅ Admin, PIC, MSD: melihat semua maintenance
            _logger.LogInformation("User {CurrentUser} with roles [{Roles}] accessing all maintenance data",
                                 currentUser, string.Join(", ", userRoles));
          }
        }

        // Apply filters
        if (request.CraneId.HasValue && request.CraneId.Value > 0)
        {
          query = query.Where(m => m.CraneId == request.CraneId.Value);
        }
        else if (!string.IsNullOrEmpty(request.CraneCode))
        {
          query = query.Where(m => m.CraneCode.Contains(request.CraneCode) ||
                                (m.Crane != null && m.Crane.Code.Contains(request.CraneCode)));
        }

        if (request.StartDate.HasValue)
        {
          var startDate = request.StartDate.Value.Date;
          query = query.Where(m => m.EndDate >= startDate);
        }

        if (request.EndDate.HasValue)
        {
          var endDate = request.EndDate.Value.Date.AddDays(1).AddSeconds(-1);
          query = query.Where(m => m.StartDate <= endDate);
        }

        // Apply global search
        if (!string.IsNullOrEmpty(request.GlobalSearch))
        {
          var search = request.GlobalSearch.ToLower();
          query = query.Where(m =>
              m.Title.ToLower().Contains(search) ||
              m.CreatedBy.ToLower().Contains(search) ||
              m.DocumentNumber.ToLower().Contains(search) ||
              (m.CraneCode != null && m.CraneCode.ToLower().Contains(search)) ||
              (m.Crane != null && m.Crane.Code.ToLower().Contains(search))
          );
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortDesc);

        // Apply pagination
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Map to view models with audit trail
        var schedules = items.Select(m => new MaintenanceScheduleViewModel
        {
          Id = m.Id,
          DocumentNumber = m.DocumentNumber,
          CraneId = m.CraneId ?? 0,
          CraneCode = m.CraneId.HasValue ? m.Crane?.Code : m.CraneCode,
          Title = m.Title,
          StartDate = m.StartDate,
          EndDate = m.EndDate,
          Description = m.Description,
          CreatedAt = m.CreatedAt,
          CreatedBy = m.CreatedBy,

          // ✅ AUDIT TRAIL FIELDS
          UpdatedAt = m.UpdatedAt,
          UpdatedBy = m.UpdatedBy
        }).ToList();

        // Calculate page count
        var pageCount = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResult<MaintenanceScheduleViewModel>
        {
          Items = schedules,
          TotalCount = totalCount,
          PageCount = pageCount,
          PageNumber = request.PageNumber,
          PageSize = request.PageSize
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting paged maintenance schedules: {Message}", ex.Message);
        throw;
      }
    }

    private IQueryable<MaintenanceSchedule> ApplySorting(IQueryable<MaintenanceSchedule> query, string sortBy, bool sortDesc)
    {
      switch (sortBy?.ToLower())
      {
        case "title":
          return sortDesc
              ? query.OrderByDescending(m => m.Title)
              : query.OrderBy(m => m.Title);
        case "cranecode":
          return sortDesc
              ? query.OrderByDescending(m => m.CraneCode)
              : query.OrderBy(m => m.CraneCode);
        case "startdate":
          return sortDesc
              ? query.OrderByDescending(m => m.StartDate)
              : query.OrderBy(m => m.StartDate);
        case "enddate":
          return sortDesc
              ? query.OrderByDescending(m => m.EndDate)
              : query.OrderBy(m => m.EndDate);
        case "createdby":
          return sortDesc
              ? query.OrderByDescending(m => m.CreatedBy)
              : query.OrderBy(m => m.CreatedBy);
        case "createdat":
        default:
          return sortDesc
              ? query.OrderByDescending(m => m.CreatedAt)
              : query.OrderBy(m => m.CreatedAt);
      }
    }

    public async Task<bool> IsShiftMaintenanceConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null)
    {
      // Delegasi ke IScheduleConflictService - pastikan parameter sesuai
      return await _scheduleConflictService.IsMaintenanceConflictAsync(craneId, date, shiftDefinitionId, excludeMaintenanceId);
    }

    public async Task<bool> MaintenanceScheduleExistsAsync(int id)
    {
      return await _context.MaintenanceSchedules.AnyAsync(m => m.Id == id);
    }

    public async Task<bool> MaintenanceScheduleExistsByDocumentNumberAsync(string documentNumber)
    {
      return await _context.MaintenanceSchedules.AnyAsync(m => m.DocumentNumber == documentNumber);
    }
  }
}
