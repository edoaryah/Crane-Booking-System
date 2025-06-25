using System;

namespace AspnetCoreMvcFull.ViewModels.Dashboard
{
  public class BreakdownHistoryItemViewModel
  {
    public int Id { get; set; }
    public string CraneCode { get; set; } = string.Empty;
    public string Reasons { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty; // "4h 12m" or "Ongoing"
  }
}
