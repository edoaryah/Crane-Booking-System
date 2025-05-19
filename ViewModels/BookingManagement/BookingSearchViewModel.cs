namespace AspnetCoreMvcFull.ViewModels.BookingManagement
{
  public class BookingSearchViewModel
  {
    public string SearchTerm { get; set; } = string.Empty;
    public IEnumerable<BookingViewModel> Bookings { get; set; } = new List<BookingViewModel>();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }
}
