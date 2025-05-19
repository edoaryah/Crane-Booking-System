// ViewModels/CraneUsage/CraneUsageFilterViewModel.cs
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class CraneUsageFilterViewModel
  {
    public int? CraneId { get; set; }
    public DateTime? StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; } = DateTime.Today.AddDays(1).AddSeconds(-1);
    public UsageCategory? Category { get; set; }
    public List<SelectListItem> CraneList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
  }
}
