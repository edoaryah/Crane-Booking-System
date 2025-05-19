// ViewModels/CraneUsage/BookingUsageFormViewModel.cs (Updated)
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class BookingUsageFormViewModel
  {
    public int BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string BookingName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string CraneCode { get; set; } = string.Empty;
    public int CraneId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;

    // Operator name has been moved to individual entries, so we remove it from here
    // public string OperatorName { get; set; } = string.Empty;

    public List<CraneUsageEntryViewModel> Entries { get; set; } = new List<CraneUsageEntryViewModel>();
  }
}
