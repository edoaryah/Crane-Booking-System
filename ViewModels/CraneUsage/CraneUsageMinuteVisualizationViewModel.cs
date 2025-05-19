// ViewModels/CraneUsage/CraneUsageMinuteVisualizationViewModel.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class CraneUsageMinuteVisualizationViewModel
  {
    [Required(ErrorMessage = "Crane harus dipilih")]
    public int CraneId { get; set; }

    [Required(ErrorMessage = "Tanggal harus diisi")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public string CraneName { get; set; } = string.Empty;

    // Data untuk visualisasi per menit
    public List<MinuteUsageData> MinuteData { get; set; } = new List<MinuteUsageData>();

    // Dropdown untuk crane
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> CraneList { get; set; } = new List<SelectListItem>();

    // Summary data
    public UsageSummary Summary { get; set; } = new UsageSummary();
  }

  public class MinuteUsageData
  {
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Category { get; set; } = "Standby"; // Default to Standby
    public string SubcategoryName { get; set; } = string.Empty;
    public string ColorCode { get; set; } = "#CCCCCC"; // Default color for Standby
    public string BookingNumber { get; set; } = string.Empty;
    public string MaintenanceTitle { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;

    // Helper properties for visualization
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;
  }

  // Di file ViewModels/CraneUsage/CraneUsageMinuteVisualizationViewModel.cs
  // Di ViewModels/CraneUsage/CraneUsageMinuteVisualizationViewModel.cs
  public class UsageSummary
  {
    // Existing properties
    public double OperatingHours { get; set; }
    public double DelayHours { get; set; }
    public double StandbyHours { get; set; }
    public double ServiceHours { get; set; }
    public double BreakdownHours { get; set; }

    public double OperatingPercentage { get; set; }
    public double DelayPercentage { get; set; }
    public double StandbyPercentage { get; set; }
    public double ServicePercentage { get; set; }
    public double BreakdownPercentage { get; set; }

    // Original computed properties
    public double AvailableHours => OperatingHours + DelayHours + StandbyHours;
    public double MaintenanceHours => ServiceHours + BreakdownHours;
    public double AvailablePercentage => OperatingPercentage + DelayPercentage + StandbyPercentage;
    public double MaintenancePercentage => ServicePercentage + BreakdownPercentage;

    // New metrics
    public double AvailabilityPercentage { get; set; } // Available Time / Calendar Time
    public double UtilisationPercentage { get; set; }  // Operating / Calendar Time
    public double UsagePercentage { get; set; }        // Utilized Time / Available Time
  }
}
