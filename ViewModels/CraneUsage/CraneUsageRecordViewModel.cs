using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  // View model for CraneUsageRecord in the list
  public class CraneUsageRecordViewModel
  {
    public int Id { get; set; }
    public int CraneId { get; set; }
    public string CraneCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsFinalized { get; set; }
    public string? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int EntryCount { get; set; }

    // Summary data
    public double TotalHours { get; set; }
    public double OperatingHours { get; set; }
    public double DelayHours { get; set; }
    public double StandbyHours { get; set; }
    public double ServiceHours { get; set; }
    public double BreakdownHours { get; set; }
  }

  // View model for the Index page
  public class CraneUsageRecordListViewModel
  {
    public List<CraneUsageRecordViewModel> Records { get; set; } = new List<CraneUsageRecordViewModel>();
    public CraneUsageFilterViewModel Filter { get; set; } = new CraneUsageFilterViewModel();
  }

  // View model for the crane selection modal
  public class CraneSelectionViewModel
  {
    [Required(ErrorMessage = "Please select a crane")]
    public int CraneId { get; set; }

    [Required(ErrorMessage = "Please select a date")]
    public DateTime Date { get; set; } = DateTime.Today;

    public List<SelectListItem> CraneList { get; set; } = new List<SelectListItem>();
  }
}
