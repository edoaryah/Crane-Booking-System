using Microsoft.AspNetCore.Mvc.Rendering;
using AspnetCoreMvcFull.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class CraneUsagePagedRequest : PagedRequest
  {
    public int? CraneId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsFinalized { get; set; } // Filter status finalisasi
    public string? GlobalSearch { get; set; }

    // For UI Display
    public List<SelectListItem> CraneList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();
  }
}
