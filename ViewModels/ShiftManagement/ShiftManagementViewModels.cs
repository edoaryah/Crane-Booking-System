using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.ShiftManagement
{
  public class ShiftViewModel
  {
    public int Id { get; set; }
    public required string Name { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; }

    // Formatted time properties for display
    public string FormattedStartTime => StartTime.ToString(@"hh\:mm");
    public string FormattedEndTime => EndTime.ToString(@"hh\:mm");
    public string TimeRange => $"{FormattedStartTime} - {FormattedEndTime}";
  }

  public class ShiftCreateViewModel
  {
    [Required(ErrorMessage = "Shift name is required")]
    [StringLength(50, ErrorMessage = "Shift name cannot exceed 50 characters")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
  }

  public class ShiftUpdateViewModel
  {
    [Required(ErrorMessage = "Shift name is required")]
    [StringLength(50, ErrorMessage = "Shift name cannot exceed 50 characters")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
  }

  public class ShiftListViewModel
  {
    public IEnumerable<ShiftViewModel> Shifts { get; set; } = new List<ShiftViewModel>();
  }
}
