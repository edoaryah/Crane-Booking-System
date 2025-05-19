using AspnetCoreMvcFull.ViewModels.CraneManagement;
using System;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.ViewModels.BookingManagement
{
  public class BookingHistoryViewModel
  {
    public IEnumerable<CraneViewModel> AvailableCranes { get; set; } = new List<CraneViewModel>();
  }
}
