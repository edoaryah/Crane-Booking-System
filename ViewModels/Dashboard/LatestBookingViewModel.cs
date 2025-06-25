using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.ViewModels.Dashboard
{
  public class LatestBookingViewModel
  {
    public int Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string CraneCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
  }
}
