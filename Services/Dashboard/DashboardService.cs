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

    public async Task<DashboardViewModel> GetDashboardDataAsync(string period)
    {
      // Konversi periode ke range tanggal
      var (startDate, endDate) = GetDateRangeForPeriod(period);

      _logger.LogInformation($"Fetching dashboard data for period: {period}, date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

      var viewModel = new DashboardViewModel
      {
        SelectedPeriod = period,
        CraneStatistics = new CraneStatisticsViewModel()
      };

      try
      {
        // Ambil semua crane
        var cranes = await _context.Cranes.ToListAsync();
        viewModel.CraneStatistics.TotalCranes = cranes.Count;
        viewModel.CraneStatistics.OperationalCranes = cranes.Count(c => c.Status == CraneStatus.Available);
        viewModel.CraneStatistics.MaintenanceCranes = cranes.Count(c => c.Status == CraneStatus.Maintenance);
        viewModel.CraneStatistics.StandbyCranes = 0; // Tidak ada status standy, bisa disesuaikan jika perlu

        // Akumulasi metrics untuk summary
        double totalAvailability = 0;
        double totalUtilisation = 0;
        double totalUsage = 0;
        int craneCount = 0;

        // Loop melalui semua tanggal dalam rentang
        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
          // Loop melalui semua crane
          foreach (var crane in cranes)
          {
            try
            {
              // Gunakan method yang sama dengan MinuteVisualization untuk mendapatkan KPI
              var visualizationData = await _craneUsageService.GetMinuteVisualizationDataAsync(crane.Id, date);

              // Jika ini tanggal pertama atau hari ini, tambahkan ke CraneMetrics untuk chart
              if (date == startDate || date == DateTime.Today)
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

    private (DateTime startDate, DateTime endDate) GetDateRangeForPeriod(string period)
    {
      DateTime now = DateTime.Now;
      DateTime startDate;
      DateTime endDate;

      switch (period?.ToLower())
      {
        case "day":
          // Hari ini
          startDate = now.Date;
          endDate = startDate; // Tampilkan data satu hari penuh
          break;
        case "week":
          // Minggu ini (mulai dari Senin)
          int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
          startDate = now.AddDays(-1 * diff).Date;
          endDate = startDate.AddDays(6);
          break;
        case "month":
        default:
          // Bulan ini
          startDate = new DateTime(now.Year, now.Month, 1);
          endDate = startDate.AddMonths(1).AddDays(-1);
          break;
      }

      // Pastikan endDate tidak melebihi hari ini
      if (endDate > now.Date)
      {
        endDate = now.Date;
      }

      return (startDate, endDate);
    }
  }
}
