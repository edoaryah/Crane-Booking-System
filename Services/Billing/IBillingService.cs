// Services/Billing/IBillingService.cs
using AspnetCoreMvcFull.ViewModels.Billing;

namespace AspnetCoreMvcFull.Services.Billing
{
  public interface IBillingService
  {
    /// <summary>
    /// Mendapatkan daftar booking yang bisa ditagih (semua yang sudah finalize)
    /// </summary>
    /// <param name="filter">Parameter filter</param>
    /// <returns>Daftar booking yang bisa ditagih</returns>
    Task<BillingListViewModel> GetBillableBookingsAsync(BillingFilterViewModel filter);

    /// <summary>
    /// Mendapatkan detail booking untuk penagihan
    /// </summary>
    /// <param name="bookingId">ID booking</param>
    /// <returns>Detail booking dan penggunaan crane</returns>
    Task<BillingDetailViewModel> GetBillingDetailAsync(int bookingId);

    /// <summary>
    /// Menandai booking sebagai sudah ditagih
    /// </summary>
    /// <param name="bookingId">ID booking</param>
    /// <param name="userName">Nama user yang menandai</param>
    /// <param name="notes">Catatan penagihan</param>
    /// <returns>True jika berhasil</returns>
    Task<bool> MarkBookingAsBilledAsync(int bookingId, string userName, string? notes);

    /// <summary>
    /// Batalkan status sudah ditagih dari booking
    /// </summary>
    /// <param name="bookingId">ID booking</param>
    /// <returns>True jika berhasil</returns>
    Task<bool> UnmarkBookingAsBilledAsync(int bookingId);
  }
}
