// ViewModels/CraneUsage/CraneUsageEntryViewModel.cs (Updated)
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class CraneUsageEntryViewModel
  {
    public int Id { get; set; }

    [Required]
    [Display(Name = "Jam Mulai")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "Jam Selesai")]
    public TimeSpan EndTime { get; set; }

    [Required]
    [Display(Name = "Kategori")]
    public UsageCategory Category { get; set; }

    [Required]
    [Display(Name = "Subkategori")]
    public int? UsageSubcategoryId { get; set; }

    [Display(Name = "Booking")]
    public int? BookingId { get; set; }

    [Display(Name = "Maintenance")]
    public int? MaintenanceScheduleId { get; set; }

    [Display(Name = "Catatan")]
    [StringLength(500)]
    public string? Notes { get; set; }

    // Tambahkan operator name di sini
    [Display(Name = "Operator")]
    [StringLength(100)]
    public string? OperatorName { get; set; }

    // Properti untuk view Index
    [Display(Name = "Crane")]
    public int CraneId { get; set; }

    [Display(Name = "Tanggal")]
    public DateTime? Date { get; set; }

    // Properti tambahan untuk tampilan
    public string? CraneName { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string SubcategoryName { get; set; } = string.Empty;
    public string? BookingNumber { get; set; }
    public string? MaintenanceTitle { get; set; }

    // Dropdown lists
    public List<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> SubcategoryList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> BookingList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> MaintenanceList { get; set; } = new List<SelectListItem>();
  }
}
