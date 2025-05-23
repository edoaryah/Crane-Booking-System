using AspnetCoreMvcFull.Models.Common;

namespace AspnetCoreMvcFull.ViewModels.BookingManagement
{
  public class BookingListPagedViewModel
  {
    public PagedResult<BookingViewModel> PagedBookings { get; set; } = new PagedResult<BookingViewModel>
    {
      Items = new List<BookingViewModel>(),
      TotalCount = 0,
      PageCount = 0,
      PageNumber = 1,
      PageSize = 10
    };
    public BookingListFilterRequest Filter { get; set; } = new BookingListFilterRequest();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }
}
