using System;
using AspnetCoreMvcFull.Models.Common;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.ViewModels.BookingManagement
{
  public class BookingListFilterRequest : PagedRequest
  {
    public int? CraneId { get; set; }
    public string? CraneCode { get; set; }
    public string? Department { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public BookingStatus? Status { get; set; }
    public string? GlobalSearch { get; set; }

    // For UI Display
    public List<SelectListItem> CraneList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> DepartmentList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();
  }
}
