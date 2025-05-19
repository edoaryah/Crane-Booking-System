using AspnetCoreMvcFull.ViewModels.CraneManagement;
using AspnetCoreMvcFull.ViewModels.ShiftManagement;

namespace AspnetCoreMvcFull.ViewModels.MaintenanceManagement
{
  public class MaintenanceFormViewModel
  {
    public IEnumerable<CraneViewModel> AvailableCranes { get; set; } = new List<CraneViewModel>();
    public IEnumerable<ShiftViewModel> ShiftDefinitions { get; set; } = new List<ShiftViewModel>();
  }
}
