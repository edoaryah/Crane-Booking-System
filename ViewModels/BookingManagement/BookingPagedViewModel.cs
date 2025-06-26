using AspnetCoreMvcFull.Models.Common;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.ViewModels.BookingManagement
{
    public class BookingPagedViewModel
    {
        // Paged result containing bookings
        public PagedResult<BookingViewModel> PagedBookings { get; set; } = new PagedResult<BookingViewModel>();

        // Search term that user entered
        public string SearchTerm { get; set; } = string.Empty;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
