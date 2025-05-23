// ViewModels/Billing/BillingViewModel.cs
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.Billing
{
  public class BillingViewModel
  {
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // Tanggal dari booking
    public DateTime BookingStartDate { get; set; }
    public DateTime BookingEndDate { get; set; }

    // Tanggal penggunaan aktual
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public string CraneCode { get; set; } = string.Empty;
    public int CraneCapacity { get; set; }
    public BookingStatus Status { get; set; }
    public double TotalHours { get; set; }
    public double OperatingHours { get; set; }
    public double DelayHours { get; set; }
    public double StandbyHours { get; set; }
    public double ServiceHours { get; set; }
    public double BreakdownHours { get; set; }
    public bool IsBilled { get; set; }
    public DateTime? BilledDate { get; set; }
    public string? BilledBy { get; set; }
    public string? BillingNotes { get; set; }
  }

  public class BillingListViewModel
  {
    public List<BillingViewModel> Bookings { get; set; } = new List<BillingViewModel>();
    public BillingFilterViewModel Filter { get; set; } = new BillingFilterViewModel();
  }

  public class BillingFilterViewModel
  {
    [Display(Name = "Status Penagihan")]
    public bool? IsBilled { get; set; }

    [Display(Name = "Tanggal Mulai")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Tanggal Akhir")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Crane")]
    public int? CraneId { get; set; }

    [Display(Name = "Departemen")]
    public string? Department { get; set; }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> CraneList { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> DepartmentList { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
  }

  public class BillingDetailViewModel
  {
    public BillingViewModel Booking { get; set; } = new BillingViewModel();
    public List<BillingEntryViewModel> Entries { get; set; } = new List<BillingEntryViewModel>();
    public BillingCalculationViewModel Calculation { get; set; } = new BillingCalculationViewModel();
  }

  public class BillingEntryViewModel
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public UsageCategory Category { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public double DurationHours { get; set; }
  }

  public class BillingCalculationViewModel
  {
    // Total hours by category
    public double TotalHours { get; set; }
    public double OperatingHours { get; set; }
    public double DelayHours { get; set; }
    public double StandbyHours { get; set; }
    public double ServiceHours { get; set; }
    public double BreakdownHours { get; set; }

    // Percentages
    public double OperatingPercentage => TotalHours > 0 ? Math.Round((OperatingHours / TotalHours) * 100, 1) : 0;
    public double DelayPercentage => TotalHours > 0 ? Math.Round((DelayHours / TotalHours) * 100, 1) : 0;
    public double StandbyPercentage => TotalHours > 0 ? Math.Round((StandbyHours / TotalHours) * 100, 1) : 0;
    public double ServicePercentage => TotalHours > 0 ? Math.Round((ServiceHours / TotalHours) * 100, 1) : 0;
    public double BreakdownPercentage => TotalHours > 0 ? Math.Round((BreakdownHours / TotalHours) * 100, 1) : 0;
  }

  // ViewModels/Billing/BillingViewModel.cs - Update MarkAsBilledViewModel

  public class MarkAsBilledViewModel
  {
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Document Number is required")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Display(Name = "Catatan Penagihan")]
    [StringLength(500, ErrorMessage = "Catatan tidak boleh lebih dari 500 karakter")]
    public string? BillingNotes { get; set; }
  }

  // TAMBAHKAN: Filter request dengan pagination
  public class BillingFilterRequest : PagedRequest
  {
    [Display(Name = "Status Penagihan")]
    public bool? IsBilled { get; set; }

    [Display(Name = "Tanggal Mulai")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Tanggal Akhir")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Crane")]
    public int? CraneId { get; set; }

    [Display(Name = "Departemen")]
    public string? Department { get; set; }

    [Display(Name = "Pencarian")]
    public string? GlobalSearch { get; set; }

    // For UI Display
    public List<SelectListItem> CraneList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> DepartmentList { get; set; } = new List<SelectListItem>();
  }

  // TAMBAHKAN: Paged view model
  public class BillingPagedViewModel
  {
    public PagedResult<BillingViewModel> PagedBookings { get; set; } = new PagedResult<BillingViewModel>();
    public BillingFilterRequest Filter { get; set; } = new BillingFilterRequest();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }
}
