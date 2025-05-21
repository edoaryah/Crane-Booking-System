using System;
using AspnetCoreMvcFull.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.ViewModels.MaintenanceManagement
{
  public class MaintenanceHistoryFilterRequest : PagedRequest
  {
    public int? CraneId { get; set; }
    public string CraneCode { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string GlobalSearch { get; set; }

    // For UI Display
    public List<SelectListItem> CraneList { get; set; } = new List<SelectListItem>();
  }
}
