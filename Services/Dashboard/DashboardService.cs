// Services/Dashboard/DashboardService.cs
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.CraneUsage;
using AspnetCoreMvcFull.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services.Dashboard
{
  public class DashboardService : IDashboardService
  {
    private readonly AppDbContext _context;
    private readonly ICraneUsageService _craneUsageService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        AppDbContext context,
        ICraneUsageService craneUsageService,
        ILogger<DashboardService> logger)
    {
      _context = context;
      _craneUsageService = craneUsageService;
      _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(string period, int? month, DateTime? startDate, DateTime? endDate)
    {
      // Konversi periode ke range tanggal
      var (dateRangeStart, dateRangeEnd) = GetDateRangeForPeriod(period, month, startDate, endDate);

      _logger.LogInformation($"Fetching dashboard data for period: {period}, month: {month}, startDate: {startDate}, endDate: {endDate}. Date range: {dateRangeStart:yyyy-MM-dd} to {dateRangeEnd:yyyy-MM-dd}");

      var viewModel = new DashboardViewModel
      {
        SelectedPeriod = period,
        SelectedMonth = month,
        StartDate = startDate,
        EndDate = endDate,
        CraneStatistics = new CraneStatisticsViewModel()
      };

      try
      {
        // Ambil semua crane
        var cranes = await _context.Cranes.ToListAsync();
        viewModel.CraneStatistics.TotalCranes = cranes.Count;
        viewModel.CraneStatistics.OperationalCranes = cranes.Count(c => c.Status == CraneStatus.Available);
        viewModel.CraneStatistics.MaintenanceCranes = cranes.Count(c => c.Status == CraneStatus.Maintenance);
        viewModel.CraneStatistics.StandbyCranes = 0; // Tidak ada status standby, bisa disesuaikan jika perlu

        // ========================
        // Booking statistics
        // ========================
        var bookings = await _context.Bookings
            .Where(b => b.StartDate.Date >= dateRangeStart && b.StartDate.Date <= dateRangeEnd)
            .ToListAsync();

        viewModel.BookingStatistics = new BookingStatisticsViewModel
        {
          TotalBookings = bookings.Count,
          CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
          OngoingBookings = bookings.Count(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Done),
          DoneBookings = bookings.Count(b => b.Status == BookingStatus.Done)
        };

        // ========================
        // Latest Bookings (limit 5)
        // ========================
        var latestBookingEntities = await _context.Bookings
            .OrderByDescending(b => b.SubmitTime)
            .Include(b => b.Crane)
            .Take(5)
            .ToListAsync();

        viewModel.LatestBookings = latestBookingEntities.Select(b => new LatestBookingViewModel
        {
          Id = b.Id,
          BookingNumber = b.BookingNumber,
          DocumentNumber = b.DocumentNumber,
          CraneCode = b.Crane != null ? b.Crane.Code : (b.CraneCode ?? "-"),
          Department = b.Department,
          Status = b.Status
        }).ToList();

        // ========================
        // Latest Breakdown Histories (limit 5)
        // ========================
        var latestBreakdowns = await _context.Breakdowns
            .OrderByDescending(br => br.UrgentStartTime)
            .Include(br => br.Crane)
            .Take(5)
            .ToListAsync();

        string FormatDuration(TimeSpan ts)
        {
          int hours = (int)ts.TotalHours;
          int minutes = ts.Minutes;
          return $"{hours}h {minutes}m";
        }

        viewModel.BreakdownHistories = latestBreakdowns.Select(br => new BreakdownHistoryItemViewModel
        {
          Id = br.Id,
          CraneCode = br.Crane != null ? br.Crane.Code : (br.CraneCode ?? "-"),
          Reasons = br.Reasons,
          DurationText = br.ActualUrgentEndTime.HasValue ? FormatDuration(br.ActualUrgentEndTime.Value - br.UrgentStartTime) : "Ongoing"
        }).ToList();

        // Akumulasi metrics untuk summary
        double totalAvailability = 0;
        double totalUtilisation = 0;
        double totalUsage = 0;
        int craneCount = 0;

        // Loop melalui semua tanggal dalam rentang
        for (DateTime date = dateRangeStart; date <= dateRangeEnd; date = date.AddDays(1))
        {
          // Loop melalui semua crane
          foreach (var crane in cranes)
          {
            try
            {
              // Gunakan method yang sama dengan Visualization untuk mendapatkan KPI
              var visualizationData = await _craneUsageService.GetVisualizationDataAsync(crane.Id, date);

              // Jika ini tanggal pertama atau hari ini, tambahkan ke CraneMetrics untuk chart
              if (date == dateRangeStart || date == DateTime.Today)
              {
                // Cek jika crane sudah ada di CraneMetrics
                var existingMetric = viewModel.CraneMetrics.FirstOrDefault(m => m.Code == crane.Code);
                if (existingMetric == null)
                {
                  // Tambahkan crane baru ke CraneMetrics
                  viewModel.CraneMetrics.Add(new CraneMetricsViewModel
                  {
                    Code = crane.Code,
                    AvailabilityPercentage = visualizationData.Summary.AvailabilityPercentage,
                    UtilisationPercentage = visualizationData.Summary.UtilisationPercentage,
                    UsagePercentage = visualizationData.Summary.UsagePercentage
                  });
                }
              }

              // Akumulasi untuk summary (rata-rata keseluruhan)
              totalAvailability += visualizationData.Summary.AvailabilityPercentage;
              totalUtilisation += visualizationData.Summary.UtilisationPercentage;
              totalUsage += visualizationData.Summary.UsagePercentage;
              craneCount++;

              _logger.LogDebug(
                  "Crane {CraneId} ({Code}) on {Date}: Avail={Avail}%, Util={Util}%, Usage={Usage}%",
                  crane.Id,
                  crane.Code,
                  date.ToString("yyyy-MM-dd"),
                  visualizationData.Summary.AvailabilityPercentage,
                  visualizationData.Summary.UtilisationPercentage,
                  visualizationData.Summary.UsagePercentage
              );
            }
            catch (Exception ex)
            {
              _logger.LogWarning(ex,
                  "Failed to get metrics for crane {CraneId} on {Date}",
                  crane.Id,
                  date.ToString("yyyy-MM-dd"));
            }
          }
        }

        // Hitung rata-rata untuk summary
        if (craneCount > 0)
        {
          viewModel.SummaryMetrics = new DashboardMetricsViewModel
          {
            AvailabilityPercentage = Math.Round(totalAvailability / craneCount, 1),
            UtilisationPercentage = Math.Round(totalUtilisation / craneCount, 1),
            UsagePercentage = Math.Round(totalUsage / craneCount, 1)
          };

          _logger.LogInformation(
              "Summary Metrics: Avail={Avail}%, Util={Util}%, Usage={Usage}%, CraneCount={Count}",
              viewModel.SummaryMetrics.AvailabilityPercentage,
              viewModel.SummaryMetrics.UtilisationPercentage,
              viewModel.SummaryMetrics.UsagePercentage,
              craneCount
          );
        }

        return viewModel;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting dashboard data for period {period}", period);
        return viewModel; // Return empty view model on error
      }
    }

    private (DateTime startDate, DateTime endDate) GetDateRangeForPeriod(string period, int? month, DateTime? customStartDate, DateTime? customEndDate)
    {
      DateTime now = DateTime.Now;
      DateTime startDate;
      DateTime endDate;

      switch (period?.ToLower())
      {
        case "day":
          startDate = now.Date;
          endDate = startDate;
          break;
        case "week":
          int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
          startDate = now.AddDays(-1 * diff).Date;
          endDate = startDate.AddDays(6);
          break;
        case "by_month":
          int targetMonth = month ?? now.Month;
          int year = now.Year;
          // Jika bulan yang dipilih lebih besar dari bulan sekarang, asumsi data yang diminta adalah tahun sebelumnya
          if (targetMonth > now.Month)
          {
            year -= 1;
          }
          startDate = new DateTime(year, targetMonth, 1);
          endDate = startDate.AddMonths(1).AddDays(-1);
          break;
        case "custom":
          startDate = customStartDate ?? now.Date;
          endDate = customEndDate ?? now.Date;
          break;
        case "month":
        default:
          startDate = new DateTime(now.Year, now.Month, 1);
          endDate = startDate.AddMonths(1).AddDays(-1);
          break;
      }

      // Pastikan endDate tidak melebihi hari ini untuk periode selain custom
      if (period?.ToLower() != "custom" && endDate > now.Date)
      {
        endDate = now.Date;
      }

      return (startDate, endDate);
    }
  }
}
