using AspnetCoreMvcFull.Models.Common;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class CraneUsageHistoryPagedViewModel
  {
    public PagedResult<CraneUsageRecordViewModel> PagedRecords { get; set; } = new PagedResult<CraneUsageRecordViewModel>();
    public CraneUsagePagedRequest Filter { get; set; } = new CraneUsagePagedRequest();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }
}
