// Services/Billing/BillingService.cs
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.Billing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models.Common;

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
      try
      {
        // Mulai dengan query dasar booking yang sudah selesai
        var baseQuery = _context.Bookings
            .Include(b => b.Crane)
            .Where(b => b.Status == BookingStatus.Done || b.Status == BookingStatus.Cancelled)
            .AsQueryable();

        // Apply filter non-tanggal terlebih dahulu
        if (filter.IsBilled.HasValue)
        {
          baseQuery = baseQuery.Where(b => b.IsBilled == filter.IsBilled.Value);
        }

        if (filter.CraneId.HasValue && filter.CraneId.Value > 0)
        {
          baseQuery = baseQuery.Where(b => b.CraneId == filter.CraneId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Department))
        {
          baseQuery = baseQuery.Where(b => b.Department.Contains(filter.Department));
        }

        // Handling filter tanggal berdasarkan usage aktual
        List<int> filteredBookingIds = new List<int>();
        bool hasDateFilter = filter.StartDate.HasValue || filter.EndDate.HasValue;

        if (hasDateFilter)
        {
          // Query untuk mendapatkan booking yang memiliki penggunaan dalam rentang tanggal
          var usageQuery = _context.CraneUsageEntries
              .Include(e => e.CraneUsageRecord)
              .Where(e => e.BookingId.HasValue && e.CraneUsageRecord.IsFinalized)
              .AsQueryable();

          // Filter berdasarkan tanggal aktual penggunaan
          if (filter.StartDate.HasValue)
          {
            usageQuery = usageQuery.Where(e => e.CraneUsageRecord.Date >= filter.StartDate.Value.Date);
          }

          if (filter.EndDate.HasValue)
          {
            usageQuery = usageQuery.Where(e => e.CraneUsageRecord.Date <= filter.EndDate.Value.Date);
          }

          // Ambil booking IDs yang memiliki penggunaan dalam rentang tanggal
          filteredBookingIds = await usageQuery
              .Select(e => e.BookingId!.Value)
              .Distinct()
              .ToListAsync();

          // Jika tidak ada booking yang memenuhi kriteria tanggal, return empty result
          if (!filteredBookingIds.Any())
          {
            return await CreateEmptyBillingListAsync(filter);
          }

          // Filter booking berdasarkan IDs yang ditemukan
          baseQuery = baseQuery.Where(b => filteredBookingIds.Contains(b.Id));
        }

        // Ambil booking yang sudah difilter
        var bookings = await baseQuery.ToListAsync();

        // Jika tidak ada booking, return empty result
        if (!bookings.Any())
        {
          return await CreateEmptyBillingListAsync(filter);
        }

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
          CraneCode = b.Crane?.Code ?? b.CraneCode ?? "Unknown",
          CraneCapacity = b.Crane?.Capacity ?? b.CraneCapacity ?? 0,
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
        })
        // Filter final jika menggunakan date filter - hanya tampilkan yang punya usage data
        .Where(vm => !hasDateFilter || (vm.ActualStartDate.HasValue && vm.ActualEndDate.HasValue))
        // Order berdasarkan tanggal aktual, fallback ke booking date
        .OrderByDescending(vm => vm.ActualEndDate ?? vm.BookingEndDate)
        .ToList();

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
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving billable bookings");
        throw;
      }
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

      return await ProcessBillingDetail(booking);
    }

    public async Task<BillingDetailViewModel> GetBillingDetailByDocumentNumberAsync(string documentNumber)
    {
      // Mendapatkan booking dengan relasi crane berdasarkan document number
      var booking = await _context.Bookings
          .Include(b => b.Crane)
          .FirstOrDefaultAsync(b => b.DocumentNumber == documentNumber);

      if (booking == null)
      {
        throw new KeyNotFoundException($"Booking dengan Document Number {documentNumber} tidak ditemukan");
      }

      return await ProcessBillingDetail(booking);
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

    // Helper method untuk memproses detail billing
    private async Task<BillingDetailViewModel> ProcessBillingDetail(Booking booking)
    {
      // Mendapatkan semua entri yang terkait booking dan sudah difinalisasi
      var entries = await _context.CraneUsageEntries
          .Include(e => e.CraneUsageRecord)
          .Include(e => e.UsageSubcategory)
          .Where(e => e.BookingId == booking.Id && e.CraneUsageRecord.IsFinalized)
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
          SubcategoryName = entry.UsageSubcategory?.Name ?? entry.SubcategoryName ?? entry.Category.ToString(),
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
        CraneCode = booking.Crane?.Code ?? booking.CraneCode ?? "Unknown",
        CraneCapacity = booking.Crane?.Capacity ?? booking.CraneCapacity ?? 0,
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

    // Helper method untuk membuat empty result
    private async Task<BillingListViewModel> CreateEmptyBillingListAsync(BillingFilterViewModel filter)
    {
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
        Bookings = new List<BillingViewModel>(),
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

    // Services/Billing/BillingService.cs
    // TAMBAHKAN method ini ke dalam class BillingService

    public async Task<PagedResult<BillingViewModel>> GetPagedBillableBookingsAsync(BillingFilterRequest request)
    {
      try
      {
        // Mulai dengan query dasar booking yang sudah selesai
        var baseQuery = _context.Bookings
            .Include(b => b.Crane)
            .Where(b => b.Status == BookingStatus.Done || b.Status == BookingStatus.Cancelled)
            .AsQueryable();

        // Apply filter non-tanggal dan non-search terlebih dahulu
        if (request.IsBilled.HasValue)
        {
          baseQuery = baseQuery.Where(b => b.IsBilled == request.IsBilled.Value);
        }

        if (request.CraneId.HasValue && request.CraneId.Value > 0)
        {
          baseQuery = baseQuery.Where(b => b.CraneId == request.CraneId.Value);
        }

        if (!string.IsNullOrEmpty(request.Department))
        {
          baseQuery = baseQuery.Where(b => b.Department.Contains(request.Department));
        }

        // Handling filter tanggal berdasarkan usage aktual
        List<int> filteredBookingIds = new List<int>();
        bool hasDateFilter = request.StartDate.HasValue || request.EndDate.HasValue;

        if (hasDateFilter)
        {
          var usageQuery = _context.CraneUsageEntries
              .Include(e => e.CraneUsageRecord)
              .Where(e => e.BookingId.HasValue && e.CraneUsageRecord.IsFinalized)
              .AsQueryable();

          if (request.StartDate.HasValue)
          {
            usageQuery = usageQuery.Where(e => e.CraneUsageRecord.Date >= request.StartDate.Value.Date);
          }

          if (request.EndDate.HasValue)
          {
            usageQuery = usageQuery.Where(e => e.CraneUsageRecord.Date <= request.EndDate.Value.Date);
          }

          filteredBookingIds = await usageQuery
              .Select(e => e.BookingId!.Value)
              .Distinct()
              .ToListAsync();

          if (!filteredBookingIds.Any())
          {
            return CreateEmptyPagedResult(request);
          }

          baseQuery = baseQuery.Where(b => filteredBookingIds.Contains(b.Id));
        }

        // Apply global search
        if (!string.IsNullOrEmpty(request.GlobalSearch))
        {
          var search = request.GlobalSearch.ToLower();
          baseQuery = baseQuery.Where(b =>
              b.BookingNumber.ToLower().Contains(search) ||
              b.Name.ToLower().Contains(search) ||
              b.Department.ToLower().Contains(search) ||
              b.DocumentNumber.ToLower().Contains(search) ||
              (b.CraneCode != null && b.CraneCode.ToLower().Contains(search)) ||
              (b.Crane != null && b.Crane.Code.ToLower().Contains(search))
          );
        }

        // Get total count before pagination
        var totalCount = await baseQuery.CountAsync();

        if (totalCount == 0)
        {
          return CreateEmptyPagedResult(request);
        }

        // Apply sorting
        baseQuery = ApplyBillingSorting(baseQuery, request.SortBy, request.SortDesc);

        // Get bookings for current page
        var bookings = await baseQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Process usage data for these bookings
        var billingViewModels = await ProcessBookingsForBilling(bookings, hasDateFilter);

        // Calculate page count
        var pageCount = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResult<BillingViewModel>
        {
          Items = billingViewModels,
          TotalCount = totalCount,
          PageCount = pageCount,
          PageNumber = request.PageNumber,
          PageSize = request.PageSize
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting paged billable bookings: {Message}", ex.Message);
        throw;
      }
    }

    private PagedResult<BillingViewModel> CreateEmptyPagedResult(BillingFilterRequest request)
    {
      return new PagedResult<BillingViewModel>
      {
        Items = new List<BillingViewModel>(),
        TotalCount = 0,
        PageCount = 0,
        PageNumber = request.PageNumber,
        PageSize = request.PageSize
      };
    }

    private IQueryable<Booking> ApplyBillingSorting(IQueryable<Booking> query, string sortBy, bool sortDesc)
    {
      switch (sortBy?.ToLower())
      {
        case "bookingnumber":
          return sortDesc
              ? query.OrderByDescending(b => b.BookingNumber)
              : query.OrderBy(b => b.BookingNumber);
        case "name":
          return sortDesc
              ? query.OrderByDescending(b => b.Name)
              : query.OrderBy(b => b.Name);
        case "department":
          return sortDesc
              ? query.OrderByDescending(b => b.Department)
              : query.OrderBy(b => b.Department);
        case "cranecode":
          return sortDesc
              ? query.OrderByDescending(b => b.CraneCode)
              : query.OrderBy(b => b.CraneCode);
        case "status":
          return sortDesc
              ? query.OrderByDescending(b => b.IsBilled)
              : query.OrderBy(b => b.IsBilled);
        case "enddate":
        default:
          return sortDesc
              ? query.OrderByDescending(b => b.EndDate)
              : query.OrderBy(b => b.EndDate);
      }
    }

    private async Task<List<BillingViewModel>> ProcessBookingsForBilling(List<Booking> bookings, bool hasDateFilter)
    {
      var bookingIds = bookings.Select(b => b.Id).ToList();
      var entries = await _context.CraneUsageEntries
          .Include(e => e.CraneUsageRecord)
          .Where(e => e.BookingId.HasValue &&
                     bookingIds.Contains(e.BookingId.Value) &&
                     e.CraneUsageRecord.IsFinalized)
          .ToListAsync();

      var bookingHoursDict = new Dictionary<int, (double TotalHours, double OperatingHours, double DelayHours,
                                                 double StandbyHours, double ServiceHours, double BreakdownHours)>();
      var bookingDatesDict = new Dictionary<int, (DateTime FirstDate, DateTime LastDate)>();

      foreach (var entry in entries)
      {
        var bookingId = entry.BookingId.Value;
        var duration = GetDurationHours(entry.StartTime, entry.EndTime);
        var usageDate = entry.CraneUsageRecord.Date;

        if (!bookingHoursDict.ContainsKey(bookingId))
        {
          bookingHoursDict[bookingId] = (0, 0, 0, 0, 0, 0);
        }

        var currentHours = bookingHoursDict[bookingId];
        var newHours = currentHours;
        newHours.TotalHours += duration;

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

      return bookings.Select(b => new BillingViewModel
      {
        BookingId = b.Id,
        BookingNumber = b.BookingNumber,
        DocumentNumber = b.DocumentNumber,
        RequesterName = b.Name,
        Department = b.Department,
        BookingStartDate = b.StartDate,
        BookingEndDate = b.EndDate,
        ActualStartDate = bookingDatesDict.ContainsKey(b.Id) ? bookingDatesDict[b.Id].FirstDate : (DateTime?)null,
        ActualEndDate = bookingDatesDict.ContainsKey(b.Id) ? bookingDatesDict[b.Id].LastDate : (DateTime?)null,
        CraneCode = b.Crane?.Code ?? b.CraneCode ?? "Unknown",
        CraneCapacity = b.Crane?.Capacity ?? b.CraneCapacity ?? 0,
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
      })
      .Where(vm => !hasDateFilter || (vm.ActualStartDate.HasValue && vm.ActualEndDate.HasValue))
      .ToList();
    }
  }
}
