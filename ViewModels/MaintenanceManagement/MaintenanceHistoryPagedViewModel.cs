using AspnetCoreMvcFull.Models.Common;

namespace AspnetCoreMvcFull.ViewModels.MaintenanceManagement
{
  public class MaintenanceHistoryPagedViewModel
  {
    public PagedResult<MaintenanceScheduleViewModel> PagedSchedules { get; set; }
    public MaintenanceHistoryFilterRequest Filter { get; set; }
    public string SuccessMessage { get; set; }
    public string ErrorMessage { get; set; }
  }
}
