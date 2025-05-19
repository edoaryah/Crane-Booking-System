// ViewModels/CraneManagement/BreakdownViewModel.cs
namespace AspnetCoreMvcFull.ViewModels.CraneManagement
{
  public class BreakdownViewModel
  {
    public int Id { get; set; }
    public int CraneId { get; set; }
    public DateTime UrgentStartTime { get; set; }
    public DateTime UrgentEndTime { get; set; }
    public DateTime? ActualUrgentEndTime { get; set; }
    public string? HangfireJobId { get; set; }
    public required string Reasons { get; set; }
  }

  public class BreakdownCreateViewModel
  {
    public DateTime UrgentStartTime { get; set; } = DateTime.Now;
    public DateTime UrgentEndTime { get; set; } = DateTime.Now.AddHours(1); // Default to 1 hour later
    public required string Reasons { get; set; }
  }

  public class BreakdownHistoryViewModel
  {
    public int Id { get; set; }
    public int CraneId { get; set; }
    public string CraneCode { get; set; } = string.Empty;
    public int CraneCapacity { get; set; }
    public DateTime UrgentStartTime { get; set; }
    public DateTime UrgentEndTime { get; set; }
    public DateTime? ActualUrgentEndTime { get; set; }
    public string Reasons { get; set; } = string.Empty;
  }
}
