// ViewModels/BookingCancellationViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels
{
  public class BookingCancellationViewModel
  {
    public int BookingId { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    public string BookingNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alasan pembatalan harus diisi")]
    [StringLength(500, ErrorMessage = "Alasan pembatalan tidak boleh lebih dari 500 karakter")]
    [Display(Name = "Alasan Pembatalan")]
    public string CancelReason { get; set; } = string.Empty;
  }
}
