// Services/Billing/BillingService.cs
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.Billing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services.Billing
{
  public class BillingService : IBillingService
  {
    private readonly AppDbContext _context;
    private readonly ILogger<BillingService> _logger;

    public BillingService(AppDbContext context, ILogger<BillingService> logger)
    {
      _context = context;
      _logger = logger;
    }

    public async Task<BillingListViewModel> GetBillableBookingsAsync(BillingFilterViewModel filter)
    {
      // Memulai dengan semua booking yang sudah selesai atau dibatalkan
      var query = _context.Bookings
          .Include(b => b.Crane)
          .Where(b => b.Status == BookingStatus.Done || b.Status == BookingStatus.Cancelled)
          .AsQueryable();

      // Menerapkan filter
      if (filter.IsBilled.HasValue)
      {
        query = query.Where(b => b.IsBilled == filter.IsBilled.Value);
      }

      if (filter.StartDate.HasValue)
      {
        query = query.Where(b => b.EndDate >= filter.StartDate.Value);
      }

      if (filter.EndDate.HasValue)
      {
        query = query.Where(b => b.StartDate <= filter.EndDate.Value);
      }

      if (filter.CraneId.HasValue && filter.CraneId.Value > 0)
      {
        query = query.Where(b => b.CraneId == filter.CraneId.Value);
      }

      if (!string.IsNullOrEmpty(filter.Department))
      {
        query = query.Where(b => b.Department.Contains(filter.Department));
      }

      // Mendapatkan booking
      var bookings = await query
          .OrderByDescending(b => b.EndDate)
          .ToListAsync();

      // Dictionary untuk menyimpan total jam per booking
      var bookingHoursDict = new Dictionary<int, (double TotalHours, double OperatingHours, double DelayHours,
                                                 double StandbyHours, double ServiceHours, double BreakdownHours)>();

      // Dictionary untuk menyimpan tanggal penggunaan aktual
      var bookingDatesDict = new Dictionary<int, (DateTime FirstDate, DateTime LastDate)>();

      // Mendapatkan semua entri yang terkait booking dan sudah difinalisasi
      var bookingIds = bookings.Select(b => b.Id).ToList();
      var entries = await _context.CraneUsageEntries
          .Include(e => e.CraneUsageRecord)
          .Where(e => e.BookingId.HasValue &&
                     bookingIds.Contains(e.BookingId.Value) &&
                     e.CraneUsageRecord.IsFinalized)
          .ToListAsync();

      // Memproses semua entri
      foreach (var entry in entries)
      {
        var bookingId = entry.BookingId.Value;
        var duration = GetDurationHours(entry.StartTime, entry.EndTime);
        var usageDate = entry.CraneUsageRecord.Date;

        // Memproses data jam
        if (!bookingHoursDict.ContainsKey(bookingId))
        {
          bookingHoursDict[bookingId] = (0, 0, 0, 0, 0, 0);
        }

        var currentHours = bookingHoursDict[bookingId];
        var newHours = currentHours;

        // Update total jam
        newHours.TotalHours += duration;

        // Update jam berdasarkan kategori
        switch (entry.Category)
        {
          case UsageCategory.Operating:
            newHours.OperatingHours += duration;
            break;
          case UsageCategory.Delay:
            newHours.DelayHours += duration;
            break;
          case UsageCategory.Standby:
            newHours.StandbyHours += duration;
            break;
          case UsageCategory.Service:
            newHours.ServiceHours += duration;
            break;
          case UsageCategory.Breakdown:
            newHours.BreakdownHours += duration;
            break;
        }

        bookingHoursDict[bookingId] = newHours;

        // Memproses data tanggal
        if (!bookingDatesDict.ContainsKey(bookingId))
        {
          bookingDatesDict[bookingId] = (usageDate, usageDate);
        }
        else
        {
          var currentDates = bookingDatesDict[bookingId];
          var newFirstDate = currentDates.FirstDate < usageDate ? currentDates.FirstDate : usageDate;
          var newLastDate = currentDates.LastDate > usageDate ? currentDates.LastDate : usageDate;
          bookingDatesDict[bookingId] = (newFirstDate, newLastDate);
        }
      }

      // Map booking ke view model
      var billingViewModels = bookings.Select(b => new BillingViewModel
      {
        BookingId = b.Id,
        BookingNumber = b.BookingNumber,
        DocumentNumber = b.DocumentNumber,
        RequesterName = b.Name,
        Department = b.Department,
        // Tanggal dari booking
        BookingStartDate = b.StartDate,
        BookingEndDate = b.EndDate,
        // Tanggal penggunaan aktual
        ActualStartDate = bookingDatesDict.ContainsKey(b.Id) ? bookingDatesDict[b.Id].FirstDate : (DateTime?)null,
        ActualEndDate = bookingDatesDict.ContainsKey(b.Id) ? bookingDatesDict[b.Id].LastDate : (DateTime?)null,
        CraneCode = b.Crane?.Code ?? "Unknown",
        CraneCapacity = b.Crane?.Capacity ?? 0,
        Status = b.Status,
        TotalHours = bookingHoursDict.ContainsKey(b.Id) ? bookingHoursDict[b.Id].TotalHours : 0,
        OperatingHours = bookingHoursDict.ContainsKey(b.Id) ? bookingHoursDict[b.Id].OperatingHours : 0,
        DelayHours = bookingHoursDict.ContainsKey(b.Id) ? bookingHoursDict[b.Id].DelayHours : 0,
        StandbyHours = bookingHoursDict.ContainsKey(b.Id) ? bookingHoursDict[b.Id].StandbyHours : 0,
        ServiceHours = bookingHoursDict.ContainsKey(b.Id) ? bookingHoursDict[b.Id].ServiceHours : 0,
        BreakdownHours = bookingHoursDict.ContainsKey(b.Id) ? bookingHoursDict[b.Id].BreakdownHours : 0,
        IsBilled = b.IsBilled,
        BilledDate = b.BilledDate,
        BilledBy = b.BilledBy,
        BillingNotes = b.BillingNotes
      }).ToList();

      // Menyiapkan data untuk filter dropdown
      var craneList = await _context.Cranes
          .OrderBy(c => c.Code)
          .Select(c => new SelectListItem
          {
            Value = c.Id.ToString(),
            Text = $"{c.Code} - {c.Capacity} Ton"
          })
          .ToListAsync();

      var departments = await _context.Bookings
          .Select(b => b.Department)
          .Distinct()
          .OrderBy(d => d)
          .Select(d => new SelectListItem
          {
            Value = d,
            Text = d
          })
          .ToListAsync();

      return new BillingListViewModel
      {
        Bookings = billingViewModels,
        Filter = new BillingFilterViewModel
        {
          IsBilled = filter.IsBilled,
          StartDate = filter.StartDate,
          EndDate = filter.EndDate,
          CraneId = filter.CraneId,
          Department = filter.Department,
          CraneList = craneList,
          DepartmentList = departments
        }
      };
    }

    public async Task<BillingDetailViewModel> GetBillingDetailAsync(int bookingId)
    {
      // Mendapatkan booking dengan relasi crane
      var booking = await _context.Bookings
          .Include(b => b.Crane)
          .FirstOrDefaultAsync(b => b.Id == bookingId);

      if (booking == null)
      {
        throw new KeyNotFoundException($"Booking dengan ID {bookingId} tidak ditemukan");
      }

      // Mendapatkan semua entri yang terkait booking dan sudah difinalisasi
      var entries = await _context.CraneUsageEntries
          .Include(e => e.CraneUsageRecord)
          .Include(e => e.UsageSubcategory)
          .Where(e => e.BookingId == bookingId && e.CraneUsageRecord.IsFinalized)
          .OrderBy(e => e.CraneUsageRecord.Date)
          .ThenBy(e => e.StartTime)
          .ToListAsync();

      // Menghitung total jam tiap kategori
      var calculation = new BillingCalculationViewModel();
      var entryViewModels = new List<BillingEntryViewModel>();

      // Hitung tanggal penggunaan aktual
      DateTime? actualStartDate = null;
      DateTime? actualEndDate = null;

      if (entries.Any())
      {
        actualStartDate = entries.Min(e => e.CraneUsageRecord.Date);
        actualEndDate = entries.Max(e => e.CraneUsageRecord.Date);
      }

      foreach (var entry in entries)
      {
        var duration = GetDurationHours(entry.StartTime, entry.EndTime);

        // Update total jam
        calculation.TotalHours += duration;

        // Update jam berdasarkan kategori
        switch (entry.Category)
        {
          case UsageCategory.Operating:
            calculation.OperatingHours += duration;
            break;
          case UsageCategory.Delay:
            calculation.DelayHours += duration;
            break;
          case UsageCategory.Standby:
            calculation.StandbyHours += duration;
            break;
          case UsageCategory.Service:
            calculation.ServiceHours += duration;
            break;
          case UsageCategory.Breakdown:
            calculation.BreakdownHours += duration;
            break;
        }

        // Tambahkan ke list entri
        entryViewModels.Add(new BillingEntryViewModel
        {
          Id = entry.Id,
          Date = entry.CraneUsageRecord.Date,
          StartTime = entry.StartTime,
          EndTime = entry.EndTime,
          Category = entry.Category,
          SubcategoryName = entry.UsageSubcategory?.Name ?? entry.Category.ToString(),
          OperatorName = entry.OperatorName ?? string.Empty,
          Notes = entry.Notes ?? string.Empty,
          DurationHours = duration
        });
      }

      // Membuat view model
      var billingViewModel = new BillingViewModel
      {
        BookingId = booking.Id,
        BookingNumber = booking.BookingNumber,
        DocumentNumber = booking.DocumentNumber,
        RequesterName = booking.Name,
        Department = booking.Department,
        BookingStartDate = booking.StartDate,
        BookingEndDate = booking.EndDate,
        ActualStartDate = actualStartDate,
        ActualEndDate = actualEndDate,
        CraneCode = booking.Crane?.Code ?? "Unknown",
        CraneCapacity = booking.Crane?.Capacity ?? 0,
        Status = booking.Status,
        TotalHours = calculation.TotalHours,
        OperatingHours = calculation.OperatingHours,
        DelayHours = calculation.DelayHours,
        StandbyHours = calculation.StandbyHours,
        ServiceHours = calculation.ServiceHours,
        BreakdownHours = calculation.BreakdownHours,
        IsBilled = booking.IsBilled,
        BilledDate = booking.BilledDate,
        BilledBy = booking.BilledBy,
        BillingNotes = booking.BillingNotes
      };

      return new BillingDetailViewModel
      {
        Booking = billingViewModel,
        Entries = entryViewModels,
        Calculation = calculation
      };
    }

    public async Task<bool> MarkBookingAsBilledAsync(int bookingId, string userName, string? notes)
    {
      try
      {
        // Mendapatkan booking
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null)
        {
          return false;
        }

        // Update data penagihan
        booking.IsBilled = true;
        booking.BilledDate = DateTime.Now;
        booking.BilledBy = userName;
        booking.BillingNotes = notes;

        await _context.SaveChangesAsync();
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error marking booking as billed");
        return false;
      }
    }

    public async Task<bool> UnmarkBookingAsBilledAsync(int bookingId)
    {
      try
      {
        // Mendapatkan booking
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null)
        {
          return false;
        }

        // Batal tandai sebagai sudah ditagih
        booking.IsBilled = false;
        booking.BilledDate = null;
        booking.BilledBy = null;
        booking.BillingNotes = null;

        await _context.SaveChangesAsync();
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error unmarking booking as billed");
        return false;
      }
    }

    // Helper method untuk menghitung durasi dalam jam
    private double GetDurationHours(TimeSpan startTime, TimeSpan endTime)
    {
      if (endTime < startTime)
      {
        // Handle kasus yang melewati tengah malam
        var duration = (new TimeSpan(24, 0, 0) - startTime) + endTime;
        return Math.Round(duration.TotalHours, 2);
      }
      else
      {
        var duration = endTime - startTime;
        return Math.Round(duration.TotalHours, 2);
      }
    }
  }
}
