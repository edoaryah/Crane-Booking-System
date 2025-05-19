using System.ComponentModel.DataAnnotations;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.CraneManagement;
using AspnetCoreMvcFull.ViewModels.ShiftManagement;

namespace AspnetCoreMvcFull.ViewModels.MaintenanceManagement
{
  public class MaintenanceScheduleViewModel
  {
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int CraneId { get; set; }
    public string? CraneCode { get; set; }
    public required string Title { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
  }

  public class MaintenanceScheduleDetailViewModel : MaintenanceScheduleViewModel
  {
    public List<MaintenanceScheduleShiftViewModel> Shifts { get; set; } = new List<MaintenanceScheduleShiftViewModel>();
  }

  public class MaintenanceScheduleShiftViewModel
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int ShiftDefinitionId { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
  }

  public class MaintenanceScheduleCreateViewModel
  {
    [Required(ErrorMessage = "Please select a crane")]
    public int CraneId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public required string Title { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public required string CreatedBy { get; set; }

    public List<DailyShiftSelectionViewModel> ShiftSelections { get; set; } = new List<DailyShiftSelectionViewModel>();
  }

  public class MaintenanceScheduleUpdateViewModel
  {
    public int CraneId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public required string Title { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public List<DailyShiftSelectionViewModel> ShiftSelections { get; set; } = new List<DailyShiftSelectionViewModel>();
  }

  public class DailyShiftSelectionViewModel
  {
    public DateTime Date { get; set; }
    public List<int> SelectedShiftIds { get; set; } = new List<int>();
  }

  public class MaintenanceListViewModel
  {
    public IEnumerable<MaintenanceScheduleViewModel> Schedules { get; set; } = new List<MaintenanceScheduleViewModel>();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }

  public class MaintenanceCalendarViewModel
  {
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CraneViewModel> Cranes { get; set; } = new List<CraneViewModel>();
    public List<ShiftViewModel> ShiftDefinitions { get; set; } = new List<ShiftViewModel>();
    public List<MaintenanceScheduleViewModel> Schedules { get; set; } = new List<MaintenanceScheduleViewModel>();
  }
}
