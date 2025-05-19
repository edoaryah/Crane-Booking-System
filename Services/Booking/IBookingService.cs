using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.BookingManagement;

namespace AspnetCoreMvcFull.Services
{
  public interface IBookingService
  {
    // Dapatkan semua booking
    Task<IEnumerable<BookingViewModel>> GetAllBookingsAsync();

    // Dapatkan booking berdasarkan ID
    Task<BookingDetailViewModel> GetBookingByIdAsync(int id);

    // Dapatkan booking berdasarkan nomor dokumen
    Task<BookingDetailViewModel> GetBookingByDocumentNumberAsync(string documentNumber);

    // Dapatkan booking berdasarkan crane ID
    Task<IEnumerable<BookingViewModel>> GetBookingsByCraneIdAsync(int craneId);

    // Dapatkan data untuk tampilan kalender
    Task<CalendarResponseViewModel> GetCalendarViewAsync(DateTime startDate, DateTime endDate);

    // Buat booking baru
    Task<BookingDetailViewModel> CreateBookingAsync(BookingCreateViewModel bookingViewModel);

    // Perbarui booking yang ada
    Task<BookingDetailViewModel> UpdateBookingAsync(int id, BookingUpdateViewModel bookingViewModel);

    // Hapus booking
    Task DeleteBookingAsync(int id);

    // Cek apakah booking ada berdasarkan ID
    Task<bool> BookingExistsAsync(int id);

    // Cek apakah booking ada berdasarkan nomor dokumen
    Task<bool> BookingExistsByDocumentNumberAsync(string documentNumber);

    // Method untuk cek konflik shift
    Task<bool> IsShiftBookingConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeBookingId = null);

    // Method untuk mendapatkan booking berdasarkan status
    Task<IEnumerable<BookingDetailViewModel>> GetBookingsByStatusAsync(BookingStatus status);

    // Method baru untuk mendapatkan shift yang sudah dibooking berdasarkan crane dan rentang tanggal
    Task<IEnumerable<BookedShiftViewModel>> GetBookedShiftsByCraneAndDateRangeAsync(int craneId, DateTime startDate, DateTime endDate);

    // Add this to the IBookingService interface (Services/Booking/IBookingService.cs)
    Task<IEnumerable<BookingViewModel>> SearchBookingsAsync(string searchTerm, string currentUser, bool isPic, bool isAdmin);
  }
}
