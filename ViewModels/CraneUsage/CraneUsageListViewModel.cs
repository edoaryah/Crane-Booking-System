// ViewModels/CraneUsage/CraneUsageListViewModel.cs
namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class CraneUsageListViewModel
  {
    public List<CraneUsageEntryViewModel> UsageRecords { get; set; } = new List<CraneUsageEntryViewModel>();
    public CraneUsageFilterViewModel Filter { get; set; } = new CraneUsageFilterViewModel();
  }
}
