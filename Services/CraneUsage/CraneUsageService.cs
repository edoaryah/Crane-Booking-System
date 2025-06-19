using AspnetCoreMvcFull.Models.Common;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.CraneUsage;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services.CraneUsage
{
  public class CraneUsageService : ICraneUsageService
  {
    private readonly AppDbContext _context;
    private readonly ILogger<CraneUsageService> _logger;

    public CraneUsageService(AppDbContext context, ILogger<CraneUsageService> logger)
    {
      _context = context;
      _logger = logger;
    }

    public async Task<List<CraneUsageEntryViewModel>> GetCraneUsageEntriesForDateAsync(int craneId, DateTime date)
    {
      // Dapatkan record untuk crane dan tanggal ini
      var record = await _context.CraneUsageRecords
          .Include(r => r.Entries)
              .ThenInclude(e => e.UsageSubcategory)
          .Include(r => r.Entries)
              .ThenInclude(e => e.Booking)
          .FirstOrDefaultAsync(r => r.CraneId == craneId && r.Date.Date == date.Date);

      if (record == null)
      {
        return new List<CraneUsageEntryViewModel>();
      }

      return record.Entries.Select(e => new CraneUsageEntryViewModel
      {
        Id = e.Id,
        StartTime = e.StartTime,
        EndTime = e.EndTime,
        Category = e.Category,
        UsageSubcategoryId = e.UsageSubcategoryId,
        BookingId = e.BookingId,
        Notes = e.Notes,
        OperatorName = e.OperatorName,
        CategoryName = e.Category.ToString(),
        // SubcategoryName = e.UsageSubcategory?.Name ?? string.Empty,
        SubcategoryName = e.UsageSubcategory?.Name ?? e.SubcategoryName ?? string.Empty,
        BookingNumber = e.Booking?.BookingNumber
      }).OrderBy(e => e.StartTime).ToList();
    }

    public async Task<bool> SaveCraneUsageFormAsync(CraneUsageFormViewModel viewModel, string userName)
    {
      // Validate that there are no time conflicts
      if (!ValidateNoTimeConflicts(viewModel.Entries))
      {
        return false;
      }

      using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        // Find or create record
        var record = await _context.CraneUsageRecords
            .FirstOrDefaultAsync(r => r.CraneId == viewModel.CraneId && r.Date.Date == viewModel.Date.Date);

        if (record == null)
        {
          // Create new record
          record = new CraneUsageRecord
          {
            CraneId = viewModel.CraneId,
            Date = viewModel.Date.Date,
            CreatedAt = DateTime.Now,
            CreatedBy = userName
          };

          _context.CraneUsageRecords.Add(record);
          await _context.SaveChangesAsync();
        }
        else if (record.IsFinalized)
        {
          // Cannot modify a finalized record
          return false;
        }

        // Get existing entries for this record
        var existingEntries = await _context.CraneUsageEntries
            .Where(e => e.CraneUsageRecordId == record.Id)
            .ToListAsync();

        // Find entries to delete (in existing but not in viewModel)
        var entriesToDelete = existingEntries
            .Where(e => !viewModel.Entries.Any(ve => ve.Id == e.Id))
            .ToList();

        // Delete entries
        foreach (var entry in entriesToDelete)
        {
          _context.CraneUsageEntries.Remove(entry);
        }

        // Find entries to update or add
        foreach (var viewModelEntry in viewModel.Entries)
        {
          if (viewModelEntry.Id > 0)
          {
            // Update existing entry
            var existingEntry = existingEntries.FirstOrDefault(e => e.Id == viewModelEntry.Id);
            if (existingEntry != null)
            {
              // Ambil subcategory untuk menyimpan nama
              UsageSubcategory? subcategory = null;
              if (viewModelEntry.UsageSubcategoryId.HasValue && viewModelEntry.UsageSubcategoryId.Value > 0)
              {
                subcategory = await _context.UsageSubcategories.FindAsync(viewModelEntry.UsageSubcategoryId.Value);
              }

              existingEntry.StartTime = viewModelEntry.StartTime;
              existingEntry.EndTime = viewModelEntry.EndTime;
              existingEntry.Category = viewModelEntry.Category;
              existingEntry.UsageSubcategoryId = viewModelEntry.UsageSubcategoryId;
              existingEntry.SubcategoryName = subcategory?.Name;
              existingEntry.BookingId = viewModelEntry.BookingId;
              existingEntry.Notes = viewModelEntry.Notes;
              existingEntry.OperatorName = viewModelEntry.OperatorName;
            }
          }
          else
          {
            // Ambil subcategory untuk menyimpan nama
            UsageSubcategory? subcategory = null;
            if (viewModelEntry.UsageSubcategoryId.HasValue && viewModelEntry.UsageSubcategoryId.Value > 0)
            {
              subcategory = await _context.UsageSubcategories.FindAsync(viewModelEntry.UsageSubcategoryId.Value);
            }

            // Add new entry
            var newEntry = new CraneUsageEntry
            {
              CraneUsageRecordId = record.Id,
              StartTime = viewModelEntry.StartTime,
              EndTime = viewModelEntry.EndTime,
              Category = viewModelEntry.Category,
              UsageSubcategoryId = viewModelEntry.UsageSubcategoryId,
              SubcategoryName = subcategory?.Name,
              BookingId = viewModelEntry.BookingId,
              Notes = viewModelEntry.Notes,
              OperatorName = viewModelEntry.OperatorName
            };

            _context.CraneUsageEntries.Add(newEntry);
          }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saving crane usage form");
        await transaction.RollbackAsync();
        return false;
      }
    }

    public async Task<bool> AddCraneUsageEntryAsync(int craneId, DateTime date, CraneUsageEntryViewModel entry, string userName)
    {
      // Find or create record
      var record = await _context.CraneUsageRecords
          .FirstOrDefaultAsync(r => r.CraneId == craneId && r.Date.Date == date.Date);

      if (record == null)
      {
        // Create new record
        record = new CraneUsageRecord
        {
          CraneId = craneId,
          Date = date.Date,
          CreatedAt = DateTime.Now,
          CreatedBy = userName
        };

        _context.CraneUsageRecords.Add(record);
        await _context.SaveChangesAsync();
      }
      else if (record.IsFinalized)
      {
        // Cannot add entries to a finalized record
        return false;
      }

      // Lakukan validasi untuk waktu
      string startTimeStr = entry.StartTime.ToString();
      string endTimeStr = entry.EndTime.ToString();

      // Pastikan format HH:mm
      if (startTimeStr.Count(c => c == ':') > 1)
        startTimeStr = string.Join(":", startTimeStr.Split(':').Take(2));
      if (endTimeStr.Count(c => c == ':') > 1)
        endTimeStr = string.Join(":", endTimeStr.Split(':').Take(2));

      TimeSpan startTime, endTime;

      // Gunakan TryParse untuk menghindari error
      if (!TimeSpan.TryParse(startTimeStr, out startTime) ||
          !TimeSpan.TryParse(endTimeStr, out endTime))
      {
        _logger.LogError($"Service - Gagal parsing waktu: {startTimeStr} atau {endTimeStr}");
        return false;
      }

      // Check for time conflicts
      var existingEntries = await GetCraneUsageEntriesForDateAsync(craneId, date);
      if (!ValidateNoTimeConflicts(existingEntries, new CraneUsageEntryViewModel
      {
        StartTime = startTime,
        EndTime = endTime,
        Id = 0 // New entry
      }))
      {
        return false; // Conflict found
      }

      // Ambil subcategory untuk menyimpan nama
      UsageSubcategory? subcategory = null;
      if (entry.UsageSubcategoryId.HasValue && entry.UsageSubcategoryId.Value > 0)
      {
        subcategory = await _context.UsageSubcategories.FindAsync(entry.UsageSubcategoryId.Value);
      }

      // Create new entry
      var newEntry = new CraneUsageEntry
      {
        CraneUsageRecordId = record.Id,
        StartTime = startTime,
        EndTime = endTime,
        Category = entry.Category,
        UsageSubcategoryId = entry.UsageSubcategoryId,
        SubcategoryName = subcategory?.Name, // Simpan nama subcategory
        BookingId = entry.BookingId,
        Notes = entry.Notes,
        OperatorName = entry.OperatorName?.Trim()  // Trim untuk menghindari whitespace
      };

      _logger.LogInformation($"Service - Adding new entry with operator name: {newEntry.OperatorName}");

      _context.CraneUsageEntries.Add(newEntry);
      await _context.SaveChangesAsync();

      return true;
    }

    public async Task<bool> UpdateCraneUsageEntryAsync(CraneUsageEntryViewModel entry)
    {
      try
      {
        _logger.LogInformation($"Service - Update entry dengan ID: {entry.Id}");
        _logger.LogInformation($"Service - Waktu yang diterima - Start: {entry.StartTime}, End: {entry.EndTime}");
        _logger.LogInformation($"Service - Operator Name: {entry.OperatorName}");

        var existingEntry = await _context.CraneUsageEntries
            .Include(e => e.CraneUsageRecord)
            .FirstOrDefaultAsync(e => e.Id == entry.Id);

        if (existingEntry == null)
        {
          _logger.LogWarning($"Service - Entry dengan ID {entry.Id} tidak ditemukan");
          return false;
        }

        if (existingEntry.CraneUsageRecord.IsFinalized)
        {
          return false; // Cannot update entries in a finalized record
        }

        // Parse waktu dengan format yang benar
        string startTimeStr = entry.StartTime.ToString();
        string endTimeStr = entry.EndTime.ToString();

        // Pastikan format HH:mm
        if (startTimeStr.Count(c => c == ':') > 1)
          startTimeStr = string.Join(":", startTimeStr.Split(':').Take(2));
        if (endTimeStr.Count(c => c == ':') > 1)
          endTimeStr = string.Join(":", endTimeStr.Split(':').Take(2));

        TimeSpan startTime, endTime;

        // Gunakan TryParse untuk menghindari error
        if (!TimeSpan.TryParse(startTimeStr, out startTime) ||
            !TimeSpan.TryParse(endTimeStr, out endTime))
        {
          _logger.LogError($"Service - Gagal parsing waktu: {startTimeStr} atau {endTimeStr}");
          return false;
        }

        _logger.LogInformation($"Service - Waktu setelah parsing - Start: {startTime}, End: {endTime}");

        // Bandingkan waktu dengan benar
        if (startTime >= endTime)
        {
          _logger.LogWarning($"Service - Validasi waktu: {startTime} >= {endTime}");
          return false;
        }

        // Tambahkan validasi konflik waktu
        var craneId = existingEntry.CraneUsageRecord.CraneId;
        var date = existingEntry.CraneUsageRecord.Date;
        var allEntries = await GetCraneUsageEntriesForDateAsync(craneId, date);

        // Convert entry to viewmodel for validation
        var entryToValidate = new CraneUsageEntryViewModel
        {
          Id = entry.Id,
          StartTime = startTime,
          EndTime = endTime,
          Category = entry.Category,
          UsageSubcategoryId = entry.UsageSubcategoryId
        };

        if (!ValidateNoTimeConflicts(allEntries, entryToValidate))
        {
          _logger.LogWarning($"Service - Konflik waktu terdeteksi");
          return false;
        }

        // Ambil subcategory untuk menyimpan nama
        UsageSubcategory? subcategory = null;
        if (entry.UsageSubcategoryId.HasValue && entry.UsageSubcategoryId.Value > 0)
        {
          subcategory = await _context.UsageSubcategories.FindAsync(entry.UsageSubcategoryId.Value);
        }

        // Update entry dengan nilai waktu yang benar
        existingEntry.StartTime = startTime;
        existingEntry.EndTime = endTime;
        existingEntry.Category = entry.Category;
        existingEntry.UsageSubcategoryId = entry.UsageSubcategoryId;
        existingEntry.SubcategoryName = subcategory?.Name;
        existingEntry.OperatorName = entry.OperatorName?.Trim();
        _logger.LogInformation($"Service - Setting operator name to: {existingEntry.OperatorName}");

        // Pastikan BookingId tetap ada
        if (entry.BookingId.HasValue)
        {
          existingEntry.BookingId = entry.BookingId;
        }

        existingEntry.Notes = entry.Notes;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Service - Entry berhasil diupdate");
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Service - Error updating crane usage entry");
        return false;
      }
    }

    public async Task<bool> DeleteCraneUsageEntryAsync(int entryId)
    {
      var entry = await _context.CraneUsageEntries
          .Include(e => e.CraneUsageRecord)
          .FirstOrDefaultAsync(e => e.Id == entryId);

      if (entry == null)
      {
        return false;
      }

      if (entry.CraneUsageRecord.IsFinalized)
      {
        return false; // Cannot delete entries from a finalized record
      }

      _context.CraneUsageEntries.Remove(entry);
      await _context.SaveChangesAsync();

      // Check if there are no more entries for this record
      var recordId = entry.CraneUsageRecordId;
      var entriesCount = await _context.CraneUsageEntries
          .CountAsync(e => e.CraneUsageRecordId == recordId);

      if (entriesCount == 0)
      {
        // Delete the record if no entries remain
        var record = await _context.CraneUsageRecords.FindAsync(recordId);
        if (record != null)
        {
          _context.CraneUsageRecords.Remove(record);
          await _context.SaveChangesAsync();
        }
      }

      return true;
    }

    public async Task<CraneUsageEntryViewModel> GetCraneUsageEntryByIdAsync(int id)
    {
      var entry = await _context.CraneUsageEntries
          .Include(e => e.UsageSubcategory)
          .Include(e => e.Booking)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (entry == null)
      {
        return new CraneUsageEntryViewModel();
      }

      return new CraneUsageEntryViewModel
      {
        Id = entry.Id,
        StartTime = entry.StartTime,
        EndTime = entry.EndTime,
        Category = entry.Category,
        UsageSubcategoryId = entry.UsageSubcategoryId,
        BookingId = entry.BookingId,
        Notes = entry.Notes,
        OperatorName = entry.OperatorName,
        CategoryName = entry.Category.ToString(),
        // SubcategoryName = entry.UsageSubcategory?.Name ?? string.Empty,
        SubcategoryName = entry.UsageSubcategory?.Name ?? entry.SubcategoryName ?? string.Empty,
        BookingNumber = entry.Booking?.BookingNumber
      };
    }

    public async Task<CraneUsageEntryViewModel> GetCraneUsageEntryByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
      var record = await _context.CraneUsageRecords
          .FirstOrDefaultAsync(r => r.CraneId == craneId && r.Date.Date == date.Date);

      if (record == null)
      {
        return new CraneUsageEntryViewModel();
      }

      var entry = await _context.CraneUsageEntries
          .Include(e => e.UsageSubcategory)
          .Include(e => e.Booking)
          .FirstOrDefaultAsync(e => e.CraneUsageRecordId == record.Id &&
                                    e.StartTime == startTime &&
                                    e.EndTime == endTime);

      if (entry == null)
      {
        return new CraneUsageEntryViewModel();
      }

      return new CraneUsageEntryViewModel
      {
        Id = entry.Id,
        StartTime = entry.StartTime,
        EndTime = entry.EndTime,
        Category = entry.Category,
        UsageSubcategoryId = entry.UsageSubcategoryId,
        BookingId = entry.BookingId,
        Notes = entry.Notes,
        OperatorName = entry.OperatorName,
        CategoryName = entry.Category.ToString(),
        // SubcategoryName = entry.UsageSubcategory?.Name ?? string.Empty,
        SubcategoryName = entry.UsageSubcategory?.Name ?? entry.SubcategoryName ?? string.Empty,
        BookingNumber = entry.Booking?.BookingNumber
      };
    }

    public bool ValidateNoTimeConflicts(List<CraneUsageEntryViewModel> existingEntries, CraneUsageEntryViewModel newEntry)
    {
      _logger.LogInformation($"Validasi konflik waktu untuk entry baru: {newEntry.StartTime} - {newEntry.EndTime}");

      // Parse waktu entry baru
      TimeSpan newStart, newEnd;

      // Ensure we have valid TimeSpan objects
      if (newEntry.StartTime is TimeSpan startTimeSpan)
        newStart = startTimeSpan;
      else if (!TimeSpan.TryParse(newEntry.StartTime.ToString(), out newStart))
      {
        _logger.LogWarning($"Format waktu start tidak valid: {newEntry.StartTime}");
        return false;
      }

      if (newEntry.EndTime is TimeSpan endTimeSpan)
        newEnd = endTimeSpan;
      else if (!TimeSpan.TryParse(newEntry.EndTime.ToString(), out newEnd))
      {
        _logger.LogWarning($"Format waktu end tidak valid: {newEntry.EndTime}");
        return false;
      }

      // Check for conflicts with existing entries
      foreach (var entry in existingEntries)
      {
        // Skip comparing to self (for edit case)
        if (entry.Id == newEntry.Id)
          continue;

        // Parse existing entry times
        TimeSpan existingStart, existingEnd;

        if (entry.StartTime is TimeSpan existingStartTimeSpan)
          existingStart = existingStartTimeSpan;
        else if (!TimeSpan.TryParse(entry.StartTime.ToString(), out existingStart))
          continue;

        if (entry.EndTime is TimeSpan existingEndTimeSpan)
          existingEnd = existingEndTimeSpan;
        else if (!TimeSpan.TryParse(entry.EndTime.ToString(), out existingEnd))
          continue;

        // Check for overlap: if new start is before existing end AND existing start is before new end
        if (newStart < existingEnd && existingStart < newEnd)
        {
          _logger.LogWarning($"Konflik waktu: Baru {newStart}-{newEnd} vs Existing {existingStart}-{existingEnd}");
          return false;
        }
      }

      return true;
    }

    public bool ValidateNoTimeConflicts(List<CraneUsageEntryViewModel> entries)
    {
      if (entries == null || !entries.Any())
        return true;

      for (int i = 0; i < entries.Count; i++)
      {
        for (int j = i + 1; j < entries.Count; j++)
        {
          TimeSpan start1, end1, start2, end2;

          // Parse times for the first entry
          if (entries[i].StartTime is TimeSpan startTimeSpan1)
            start1 = startTimeSpan1;
          else if (!TimeSpan.TryParse(entries[i].StartTime.ToString(), out start1))
            continue;

          if (entries[i].EndTime is TimeSpan endTimeSpan1)
            end1 = endTimeSpan1;
          else if (!TimeSpan.TryParse(entries[i].EndTime.ToString(), out end1))
            continue;

          // Parse times for the second entry
          if (entries[j].StartTime is TimeSpan startTimeSpan2)
            start2 = startTimeSpan2;
          else if (!TimeSpan.TryParse(entries[j].StartTime.ToString(), out start2))
            continue;

          if (entries[j].EndTime is TimeSpan endTimeSpan2)
            end2 = endTimeSpan2;
          else if (!TimeSpan.TryParse(entries[j].EndTime.ToString(), out end2))
            continue;

          // Check for overlap: if start1 is before end2 AND start2 is before end1
          if (start1 < end2 && start2 < end1)
          {
            _logger.LogWarning($"Konflik waktu: Entry {i} {start1}-{end1} vs Entry {j} {start2}-{end2}");
            return false;
          }
        }
      }

      return true;
    }

    private bool TimeSpansOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
    {
      return start1 < end2 && start2 < end1;
    }

    public async Task<List<SelectListItem>> GetSubcategoriesByCategoryAsync(UsageCategory category)
    {
      var subcategories = await _context.UsageSubcategories
          .Where(s => s.Category == category && s.IsActive)
          .OrderBy(s => s.Name)
          .Select(s => new SelectListItem
          {
            Value = s.Id.ToString(),
            Text = s.Name
          })
          .ToListAsync();

      return subcategories;
    }

    public async Task<List<SelectListItem>> GetAvailableBookingsAsync(int craneId, DateTime startTime, DateTime endTime)
    {
      var bookings = await _context.Bookings
          .Where(b => b.CraneId == craneId &&
                     b.Status == BookingStatus.PICApproved &&
                     ((b.StartDate <= startTime && b.EndDate >= startTime) ||
                     (b.StartDate <= endTime && b.EndDate >= endTime) ||
                     (b.StartDate >= startTime && b.EndDate <= endTime)))
          .Select(b => new SelectListItem
          {
            Value = b.Id.ToString(),
            Text = $"{b.BookingNumber} - {b.Name} ({b.StartDate:dd/MM/yyyy HH:mm} - {b.EndDate:dd/MM/yyyy HH:mm})"
          })
          .ToListAsync();

      return bookings;
    }

    public async Task<bool> FinalizeRecordAsync(int craneId, DateTime date, string userName)
    {
      try
      {
        // Find the record
        var record = await _context.CraneUsageRecords
            .FirstOrDefaultAsync(r => r.CraneId == craneId && r.Date.Date == date.Date);

        // Check if there are any entries for this date and crane
        var hasEntries = await _context.CraneUsageEntries
            .Include(e => e.CraneUsageRecord)
            .AnyAsync(e => e.CraneUsageRecord.CraneId == craneId && e.CraneUsageRecord.Date.Date == date.Date);

        if (!hasEntries)
        {
          return false; // Don't finalize if there are no entries
        }

        if (record == null)
        {
          // Record doesn't exist, create it
          record = new CraneUsageRecord
          {
            CraneId = craneId,
            Date = date.Date,
            CreatedBy = userName,
            CreatedAt = DateTime.Now
          };
          _context.CraneUsageRecords.Add(record);
        }

        // Set finalization properties
        record.IsFinalized = true;
        record.FinalizedBy = userName;
        record.FinalizedAt = DateTime.Now;

        // Save changes
        await _context.SaveChangesAsync();
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error finalizing record for crane ID {CraneId} on date {Date}", craneId, date);
        return false;
      }
    }

    public async Task<CraneUsageRecordListViewModel> GetFilteredUsageRecordsAsync(CraneUsageFilterViewModel filter)
    {
      // Default filter values if not provided
      if (!filter.StartDate.HasValue)
      {
        filter.StartDate = DateTime.Today.AddDays(-30);
      }

      if (!filter.EndDate.HasValue)
      {
        filter.EndDate = DateTime.Today;
      }

      // Get the crane list for the filter dropdown
      var cranes = await _context.Cranes
          .OrderBy(c => c.Code)
          .Select(c => new SelectListItem
          {
            Value = c.Id.ToString(),
            Text = $"{c.Code} - {c.Capacity} Ton"
          })
          .ToListAsync();

      // Start with all records
      var query = _context.CraneUsageRecords
          .Include(r => r.Crane)
          .Include(r => r.Entries)
          .AsQueryable();

      // Apply filters
      if (filter.CraneId.HasValue && filter.CraneId.Value > 0)
      {
        query = query.Where(r => r.CraneId == filter.CraneId.Value);
      }

      if (filter.StartDate.HasValue)
      {
        query = query.Where(r => r.Date >= filter.StartDate.Value.Date);
      }

      if (filter.EndDate.HasValue)
      {
        query = query.Where(r => r.Date <= filter.EndDate.Value.Date);
      }

      // Get the records
      var records = await query
          .OrderByDescending(r => r.Date)
          .ToListAsync();

      // Map to view models
      var recordViewModels = records.Select(r => new CraneUsageRecordViewModel
      {
        Id = r.Id,
        CraneId = r.CraneId,
        CraneCode = r.Crane?.Code ?? "Unknown",
        Date = r.Date,
        IsFinalized = r.IsFinalized,
        FinalizedBy = r.FinalizedBy,
        FinalizedAt = r.FinalizedAt,
        EntryCount = r.Entries.Count,
        TotalHours = CalculateTotalHours(r.Entries),
        OperatingHours = CalculateCategoryHours(r.Entries, UsageCategory.Operating),
        DelayHours = CalculateCategoryHours(r.Entries, UsageCategory.Delay),
        StandbyHours = CalculateCategoryHours(r.Entries, UsageCategory.Standby),
        ServiceHours = CalculateCategoryHours(r.Entries, UsageCategory.Service),
        BreakdownHours = CalculateCategoryHours(r.Entries, UsageCategory.Breakdown)
      }).ToList();

      // Create category list for the filter dropdown
      var categoryList = Enum.GetValues(typeof(UsageCategory))
          .Cast<UsageCategory>()
          .Select(c => new SelectListItem
          {
            Value = ((int)c).ToString(),
            Text = c.ToString()
          })
          .ToList();

      // Return the view model
      return new CraneUsageRecordListViewModel
      {
        Records = recordViewModels,
        Filter = new CraneUsageFilterViewModel
        {
          CraneId = filter.CraneId,
          StartDate = filter.StartDate,
          EndDate = filter.EndDate,
          CraneList = cranes,
          CategoryList = categoryList
        }
      };
    }

    // Helper methods for calculating hours
    private double CalculateTotalHours(ICollection<CraneUsageEntry> entries)
    {
      double totalHours = 0;
      foreach (var entry in entries)
      {
        TimeSpan duration;
        if (entry.EndTime > entry.StartTime)
        {
          duration = entry.EndTime - entry.StartTime;
        }
        else
        {
          // Handle entries that span midnight
          duration = (new TimeSpan(24, 0, 0) - entry.StartTime) + entry.EndTime;
        }
        totalHours += duration.TotalHours;
      }
      return totalHours;
    }

    private double CalculateCategoryHours(ICollection<CraneUsageEntry> entries, UsageCategory category)
    {
      double categoryHours = 0;
      foreach (var entry in entries.Where(e => e.Category == category))
      {
        TimeSpan duration;
        if (entry.EndTime > entry.StartTime)
        {
          duration = entry.EndTime - entry.StartTime;
        }
        else
        {
          // Handle entries that span midnight
          duration = (new TimeSpan(24, 0, 0) - entry.StartTime) + entry.EndTime;
        }
        categoryHours += duration.TotalHours;
      }
      return categoryHours;
    }

    private string GetCategoryColorCode(UsageCategory category)
    {
      return category switch
      {
        UsageCategory.Operating => "#28a745", // Green
        UsageCategory.Delay => "#ffc107",     // Yellow
        UsageCategory.Standby => "#6c757d",   // Gray
        UsageCategory.Service => "#17a2b8",   // Cyan
        UsageCategory.Breakdown => "#dc3545", // Red
        _ => "#6c757d"                        // Default Gray
      };
    }

    public async Task<bool> SaveBookingUsageFormAsync(BookingUsageFormViewModel viewModel, string userName)
    {
      // Validate no time conflicts (reuse existing logic)
      if (!ValidateNoTimeConflicts(viewModel.Entries))
      {
        return false;
      }

      using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        // Find or create record
        var record = await _context.CraneUsageRecords
            .FirstOrDefaultAsync(r => r.CraneId == viewModel.CraneId && r.Date.Date == viewModel.Date.Date);

        if (record == null)
        {
          // Create new record
          record = new CraneUsageRecord
          {
            CraneId = viewModel.CraneId,
            Date = viewModel.Date.Date,
            CreatedAt = DateTime.Now,
            CreatedBy = userName
          };

          _context.CraneUsageRecords.Add(record);
          await _context.SaveChangesAsync();
        }
        else if (record.IsFinalized)
        {
          // Cannot modify a finalized record
          return false;
        }

        // Process entries - handle deletes, updates, and inserts
        var existingEntries = await _context.CraneUsageEntries
            .Where(e => e.CraneUsageRecordId == record.Id && e.BookingId == viewModel.BookingId)
            .ToListAsync();

        // Delete entries not in viewModel
        var entriesToDelete = existingEntries
            .Where(e => !viewModel.Entries.Any(ve => ve.Id == e.Id))
            .ToList();

        foreach (var entry in entriesToDelete)
        {
          _context.CraneUsageEntries.Remove(entry);
        }

        // Update or add entries
        foreach (var viewModelEntry in viewModel.Entries)
        {
          // Ensure BookingId is set
          viewModelEntry.BookingId = viewModel.BookingId;

          if (viewModelEntry.Id > 0)
          {
            // Update existing entry
            var existingEntry = existingEntries.FirstOrDefault(e => e.Id == viewModelEntry.Id);
            if (existingEntry != null)
            {
              // Ambil subcategory untuk menyimpan nama
              UsageSubcategory? subcategory = null;
              if (viewModelEntry.UsageSubcategoryId.HasValue && viewModelEntry.UsageSubcategoryId.Value > 0)
              {
                subcategory = await _context.UsageSubcategories.FindAsync(viewModelEntry.UsageSubcategoryId.Value);
              }

              existingEntry.StartTime = viewModelEntry.StartTime;
              existingEntry.EndTime = viewModelEntry.EndTime;
              existingEntry.Category = viewModelEntry.Category;
              existingEntry.UsageSubcategoryId = viewModelEntry.UsageSubcategoryId;
              existingEntry.SubcategoryName = subcategory?.Name;
              existingEntry.BookingId = viewModelEntry.BookingId;
              existingEntry.Notes = viewModelEntry.Notes;
              existingEntry.OperatorName = viewModelEntry.OperatorName;
            }
          }
          else
          {
            // Ambil subcategory untuk menyimpan nama
            UsageSubcategory? subcategory = null;
            if (viewModelEntry.UsageSubcategoryId.HasValue && viewModelEntry.UsageSubcategoryId.Value > 0)
            {
              subcategory = await _context.UsageSubcategories.FindAsync(viewModelEntry.UsageSubcategoryId.Value);
            }

            // Add new entry
            var newEntry = new CraneUsageEntry
            {
              CraneUsageRecordId = record.Id,
              StartTime = viewModelEntry.StartTime,
              EndTime = viewModelEntry.EndTime,
              Category = viewModelEntry.Category,
              UsageSubcategoryId = viewModelEntry.UsageSubcategoryId,
              SubcategoryName = subcategory?.Name, // Simpan nama subcategory
              BookingId = viewModelEntry.BookingId,
              Notes = viewModelEntry.Notes,
              OperatorName = viewModelEntry.OperatorName
            };

            _context.CraneUsageEntries.Add(newEntry);
          }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saving booking usage form");
        await transaction.RollbackAsync();
        return false;
      }
    }

        public async Task<CraneUsageVisualizationViewModel> GetVisualizationDataAsync(int craneId, DateTime date)
    {
      var crane = await _context.Cranes.FindAsync(craneId);
      if (crane == null)
      {
        throw new KeyNotFoundException($"Crane with ID {craneId} not found");
      }

            var viewModel = new CraneUsageVisualizationViewModel
      {
        CraneId = craneId,
        Date = date,
        CraneName = crane.Code,
        CraneList = await _context.Cranes
              .OrderBy(c => c.Code)
              .Select(c => new SelectListItem
              {
                Value = c.Id.ToString(),
                Text = $"{c.Code} - {c.Capacity} Ton"
              })
              .ToListAsync()
      };

      // Get all entries for this crane and date with precise time data
      var entries = await _context.CraneUsageEntries
          .Include(e => e.CraneUsageRecord)
          .Include(e => e.UsageSubcategory)
          .Include(e => e.Booking)
          .Where(e => e.CraneUsageRecord != null &&
                     e.CraneUsageRecord.CraneId == craneId &&
                     e.CraneUsageRecord.Date.Date == date.Date)
          .OrderBy(e => e.StartTime)
          .ToListAsync();

      // Convert to minute-level data
      var minuteData = new List<MinuteUsageData>();
      DateTime dayStart = date.Date;

      // Create standby entries for any gaps
      TimeSpan currentTime = new TimeSpan(0, 0, 0); // 00:00:00
      TimeSpan endOfDay = new TimeSpan(24, 0, 0); // 24:00:00

      foreach (var entry in entries)
      {
        // If there's a gap before this entry, add a Standby entry
        if (entry.StartTime > currentTime)
        {
          minuteData.Add(new MinuteUsageData
          {
            StartTime = dayStart.Add(currentTime),
            EndTime = dayStart.Add(entry.StartTime),
            Category = "Standby",
            SubcategoryName = "Standby",
            ColorCode = GetCategoryColorCode(UsageCategory.Standby)
          });
        }

        // Add the actual entry
        minuteData.Add(new MinuteUsageData
        {
          StartTime = dayStart.Add(entry.StartTime),
          EndTime = dayStart.Add(entry.EndTime),
          Category = entry.Category.ToString(),
          SubcategoryName = entry.UsageSubcategory?.Name ?? entry.Category.ToString(),
          ColorCode = GetCategoryColorCode(entry.Category),
          BookingNumber = entry.Booking?.BookingNumber ?? string.Empty,
          Notes = entry.Notes ?? string.Empty,
          OperatorName = entry.OperatorName ?? string.Empty
        });

        // Update current time pointer
        currentTime = entry.EndTime;
      }

      // If there's remaining time in the day, add final Standby entry
      if (currentTime < endOfDay)
      {
        minuteData.Add(new MinuteUsageData
        {
          StartTime = dayStart.Add(currentTime),
          EndTime = dayStart.Add(endOfDay),
          Category = "Standby",
          SubcategoryName = "Standby",
          ColorCode = GetCategoryColorCode(UsageCategory.Standby)
        });
      }

      viewModel.MinuteData = minuteData;

      // Calculate summary
      var summary = new UsageSummary();
      double totalMinutes = 24 * 60; // Total minutes in a day

      // Calculate totals by category
      foreach (var item in minuteData)
      {
        double durationMinutes = item.DurationMinutes;

        switch (item.Category)
        {
          case "Operating":
            summary.OperatingHours += durationMinutes / 60;
            break;
          case "Delay":
            summary.DelayHours += durationMinutes / 60;
            break;
          case "Standby":
            summary.StandbyHours += durationMinutes / 60;
            break;
          case "Service":
            summary.ServiceHours += durationMinutes / 60;
            break;
          case "Breakdown":
            summary.BreakdownHours += durationMinutes / 60;
            break;
        }
      }

      // Calculate original percentages
      summary.OperatingPercentage = Math.Round((summary.OperatingHours * 60 / totalMinutes) * 100, 1);
      summary.DelayPercentage = Math.Round((summary.DelayHours * 60 / totalMinutes) * 100, 1);
      summary.StandbyPercentage = Math.Round((summary.StandbyHours * 60 / totalMinutes) * 100, 1);
      summary.ServicePercentage = Math.Round((summary.ServiceHours * 60 / totalMinutes) * 100, 1);
      summary.BreakdownPercentage = Math.Round((summary.BreakdownHours * 60 / totalMinutes) * 100, 1);

      // Calculate new metrics
      double calendarHours = summary.OperatingHours + summary.DelayHours + summary.StandbyHours +
                              summary.ServiceHours + summary.BreakdownHours; // Total 24 jam
      double availableHours = summary.OperatingHours + summary.DelayHours + summary.StandbyHours;
      double utilizedHours = summary.OperatingHours + summary.DelayHours;

      // Availability = Available Time / Calendar Time
      if (calendarHours > 0)
      {
        summary.AvailabilityPercentage = Math.Round((availableHours / calendarHours) * 100, 1);

        // Utilisation = Operating / Calendar Time
        summary.UtilisationPercentage = Math.Round((summary.OperatingHours / calendarHours) * 100, 1);
      }
      else
      {
        summary.AvailabilityPercentage = 0;
        summary.UtilisationPercentage = 0;
      }

      // Usage = Utilized Time / Available Time
      if (availableHours > 0)
      {
        summary.UsagePercentage = Math.Round((utilizedHours / availableHours) * 100, 1);
      }
      else
      {
        summary.UsagePercentage = 0;
      }

      viewModel.Summary = summary;

      return viewModel;
    }

    // Tambahkan method ini di dalam class CraneUsageService

    public async Task<PagedResult<CraneUsageRecordViewModel>> GetPagedCraneUsageRecordsAsync(CraneUsagePagedRequest request)
    {
      try
      {
        // Start with all records
        var query = _context.CraneUsageRecords
            .Include(r => r.Crane)
            .Include(r => r.Entries)
            .AsQueryable();

        // Apply filters
        if (request.CraneId.HasValue && request.CraneId.Value > 0)
        {
          query = query.Where(r => r.CraneId == request.CraneId.Value);
        }

        if (request.StartDate.HasValue)
        {
          var startDate = request.StartDate.Value.Date;
          query = query.Where(r => r.Date >= startDate);
        }

        if (request.EndDate.HasValue)
        {
          var endDate = request.EndDate.Value.Date;
          query = query.Where(r => r.Date <= endDate);
        }

        if (request.IsFinalized.HasValue)
        {
          query = query.Where(r => r.IsFinalized == request.IsFinalized.Value);
        }

        // Apply global search
        if (!string.IsNullOrEmpty(request.GlobalSearch))
        {
          var search = request.GlobalSearch.ToLower();
          query = query.Where(r =>
              (r.Crane != null && r.Crane.Code.ToLower().Contains(search)) ||
              (r.FinalizedBy != null && r.FinalizedBy.ToLower().Contains(search)) ||
              (r.CreatedBy.ToLower().Contains(search))
          );
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplyCraneUsageSorting(query, request.SortBy, request.SortDesc);

        // Apply pagination
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Map to view models
        var records = items.Select(r => new CraneUsageRecordViewModel
        {
          Id = r.Id,
          CraneId = r.CraneId,
          CraneCode = r.Crane?.Code ?? "Unknown",
          Date = r.Date,
          IsFinalized = r.IsFinalized,
          FinalizedBy = r.FinalizedBy,
          FinalizedAt = r.FinalizedAt,
          EntryCount = r.Entries.Count,
          TotalHours = CalculateTotalHours(r.Entries),
          OperatingHours = CalculateCategoryHours(r.Entries, UsageCategory.Operating),
          DelayHours = CalculateCategoryHours(r.Entries, UsageCategory.Delay),
          StandbyHours = CalculateCategoryHours(r.Entries, UsageCategory.Standby),
          ServiceHours = CalculateCategoryHours(r.Entries, UsageCategory.Service),
          BreakdownHours = CalculateCategoryHours(r.Entries, UsageCategory.Breakdown)
        }).ToList();

        // Calculate page count
        var pageCount = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        // Create and return paged result
        return new PagedResult<CraneUsageRecordViewModel>
        {
          Items = records,
          TotalCount = totalCount,
          PageCount = pageCount,
          PageNumber = request.PageNumber,
          PageSize = request.PageSize
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting paged crane usage records: {Message}", ex.Message);
        throw;
      }
    }

    private IQueryable<CraneUsageRecord> ApplyCraneUsageSorting(IQueryable<CraneUsageRecord> query, string sortBy, bool sortDesc)
    {
      switch (sortBy?.ToLower())
      {
        case "crane":
        case "cranecode":
          return sortDesc
              ? query.OrderByDescending(r => r.Crane != null ? r.Crane.Code : "")
              : query.OrderBy(r => r.Crane != null ? r.Crane.Code : "");
        case "date":
          return sortDesc
              ? query.OrderByDescending(r => r.Date)
              : query.OrderBy(r => r.Date);
        case "isfinalized":
        case "status":
          return sortDesc
              ? query.OrderByDescending(r => r.IsFinalized)
              : query.OrderBy(r => r.IsFinalized);
        case "finalizedby":
          return sortDesc
              ? query.OrderByDescending(r => r.FinalizedBy ?? "")
              : query.OrderBy(r => r.FinalizedBy ?? "");
        case "createdat":
        default:
          return sortDesc
              ? query.OrderByDescending(r => r.CreatedAt)
              : query.OrderBy(r => r.CreatedAt);
      }
    }
  }
}
