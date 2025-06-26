// ViewModels/Dashboard/DashboardViewModel.cs
using System.Collections.Generic;
using System;
namespace AspnetCoreMvcFull.ViewModels.Dashboard
{
  public class DashboardViewModel
  {
    public string SelectedPeriod { get; set; } = "month"; // Default: bulan ini
    public int? SelectedMonth { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Summary metrics untuk seluruh crane
    public DashboardMetricsViewModel SummaryMetrics { get; set; } = new DashboardMetricsViewModel();

    // Data crane individu untuk chart
    public List<CraneMetricsViewModel> CraneMetrics { get; set; } = new List<CraneMetricsViewModel>();

    // Statistik crane
    public CraneStatisticsViewModel CraneStatistics { get; set; } = new CraneStatisticsViewModel();

    // Statistik booking
    public BookingStatisticsViewModel BookingStatistics { get; set; } = new BookingStatisticsViewModel();

    // Booking terbaru
    public List<LatestBookingViewModel> LatestBookings { get; set; } = new List<LatestBookingViewModel>();

    // Riwayat breakdown terbaru
    public List<BreakdownHistoryItemViewModel> BreakdownHistories { get; set; } = new List<BreakdownHistoryItemViewModel>();
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

  public class BookingStatisticsViewModel
  {
    public int TotalBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int OngoingBookings { get; set; }
    public int DoneBookings { get; set; }

    public double CancelledPercentage => TotalBookings > 0 ? (double)CancelledBookings / TotalBookings * 100 : 0;
    public double OngoingPercentage => TotalBookings > 0 ? (double)OngoingBookings / TotalBookings * 100 : 0;
    public double DonePercentage => TotalBookings > 0 ? (double)DoneBookings / TotalBookings * 100 : 0;
  }
}
