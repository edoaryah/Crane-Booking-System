// ViewModels/Dashboard/DashboardViewModel.cs
namespace AspnetCoreMvcFull.ViewModels.Dashboard
{
  public class DashboardViewModel
  {
    public string SelectedPeriod { get; set; } = "month"; // Default: bulan ini

    // Summary metrics untuk seluruh crane
    public DashboardMetricsViewModel SummaryMetrics { get; set; } = new DashboardMetricsViewModel();

    // Data crane individu untuk chart
    public List<CraneMetricsViewModel> CraneMetrics { get; set; } = new List<CraneMetricsViewModel>();

    // Statistik crane
    public CraneStatisticsViewModel CraneStatistics { get; set; } = new CraneStatisticsViewModel();
  }

  public class DashboardMetricsViewModel
  {
    public double AvailabilityPercentage { get; set; }
    public double UtilisationPercentage { get; set; }
    public double UsagePercentage { get; set; }
  }

  public class CraneMetricsViewModel
  {
    public string Code { get; set; } = string.Empty;
    public double AvailabilityPercentage { get; set; }
    public double UtilisationPercentage { get; set; }
    public double UsagePercentage { get; set; }
  }

  public class CraneStatisticsViewModel
  {
    public int TotalCranes { get; set; }
    public int OperationalCranes { get; set; }
    public int StandbyCranes { get; set; }
    public int MaintenanceCranes { get; set; }

    public double OperationalPercentage => TotalCranes > 0 ? (double)OperationalCranes / TotalCranes * 100 : 0;
    public double StandbyPercentage => TotalCranes > 0 ? (double)StandbyCranes / TotalCranes * 100 : 0;
    public double MaintenancePercentage => TotalCranes > 0 ? (double)MaintenanceCranes / TotalCranes * 100 : 0;
  }
}
