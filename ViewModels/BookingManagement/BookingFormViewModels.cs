using System.ComponentModel.DataAnnotations;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.CraneManagement;
using AspnetCoreMvcFull.ViewModels.HazardManagement;
using AspnetCoreMvcFull.ViewModels.ShiftManagement;

namespace AspnetCoreMvcFull.ViewModels.BookingManagement
{
  // ViewModel untuk tampilan form booking
  public class BookingFormViewModel
  {
    public IEnumerable<CraneViewModel> AvailableCranes { get; set; } = new List<CraneViewModel>();
    public IEnumerable<ShiftViewModel> ShiftDefinitions { get; set; } = new List<ShiftViewModel>();
    public IEnumerable<HazardViewModel> AvailableHazards { get; set; } = new List<HazardViewModel>();

    // Properti untuk menyimpan data shift yang telah dibooking
    public IEnumerable<BookedShiftViewModel> BookedShifts { get; set; } = new List<BookedShiftViewModel>();
  }

  // ViewModel untuk daftar booking
  public class BookingListViewModel
  {
    // Dalam BookingListViewModel
    public IEnumerable<BookingViewModel> Bookings { get; set; } = Enumerable.Empty<BookingViewModel>();
    public string? Title { get; set; } = "Booking List";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }

  // ViewModel untuk menampilkan informasi dasar booking
  // ViewModels/BookingManagement/BookingFormViewModels.cs
  public class BookingViewModel
  {
    public int Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int CraneId { get; set; } // Tetap int karena ViewModel tidak perlu nullable
    public string? CraneCode { get; set; } // Sudah ada untuk data historis
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SubmitTime { get; set; }
    public string? Location { get; set; }
    public BookingStatus Status { get; set; }
    public string? ProjectSupervisor { get; set; }
    public string? CostCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    // Tambahkan properti untuk data historis lainnya jika diperlukan
    public int? CraneCapacity { get; set; }
    public CraneOwnership? CraneOwnership { get; set; }
  }

  // ViewModel untuk menampilkan detail booking dengan semua informasi terkait
  public class BookingDetailViewModel : BookingViewModel
  {
    // Status approval
    // public BookingStatus Status { get; set; }

    // Manager approval info
    public string? ManagerName { get; set; }
    public DateTime? ManagerApprovalTime { get; set; }
    public string? ManagerRejectReason { get; set; }

    // PIC approval info
    public string? ApprovedByPIC { get; set; }
    public DateTime? ApprovedAtByPIC { get; set; }
    public string? PICRejectReason { get; set; }

    // PIC completion info
    public string? DoneByPIC { get; set; }
    public DateTime? DoneAt { get; set; }

    // Cancellation properties
    public BookingCancelledBy CancelledBy { get; set; }
    public string? CancelledByName { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledReason { get; set; }

    // Revision tracking
    public int RevisionCount { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;

    public List<BookingShiftViewModel> Shifts { get; set; } = new List<BookingShiftViewModel>();
    public List<BookingItemViewModel> Items { get; set; } = new List<BookingItemViewModel>();
    public List<HazardViewModel> SelectedHazards { get; set; } = new List<HazardViewModel>();
    public string? CustomHazard { get; set; }
  }

  // ViewModel untuk shift dalam booking
  public class BookingShiftViewModel
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int ShiftDefinitionId { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
  }

  // ViewModel untuk item dalam booking
  public class BookingItemViewModel
  {
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public int Quantity { get; set; }
  }

  // ViewModel untuk membuat booking baru
  public class BookingCreateViewModel
  {
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Crane is required")]
    public int CraneId { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
    public string? Location { get; set; }

    [StringLength(100, ErrorMessage = "Project supervisor cannot exceed 100 characters")]
    public string? ProjectSupervisor { get; set; }

    [StringLength(50, ErrorMessage = "Cost code cannot exceed 50 characters")]
    public string? CostCode { get; set; }

    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public List<DailyShiftSelectionViewModel> ShiftSelections { get; set; } = new List<DailyShiftSelectionViewModel>();
    public List<BookingItemCreateViewModel> Items { get; set; } = new List<BookingItemCreateViewModel>();
    public List<int>? HazardIds { get; set; } = new List<int>();
    public string? CustomHazard { get; set; }
  }

  // ViewModel untuk memperbarui booking yang ada
  public class BookingUpdateViewModel
  {
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Crane is required")]
    public int CraneId { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
    public string? Location { get; set; }

    [StringLength(100, ErrorMessage = "Project supervisor cannot exceed 100 characters")]
    public string? ProjectSupervisor { get; set; }

    [StringLength(50, ErrorMessage = "Cost code cannot exceed 50 characters")]
    public string? CostCode { get; set; }

    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public List<DailyShiftSelectionViewModel> ShiftSelections { get; set; } = new List<DailyShiftSelectionViewModel>();
    public List<BookingItemCreateViewModel> Items { get; set; } = new List<BookingItemCreateViewModel>();
    public List<int>? HazardIds { get; set; } = new List<int>();
    public string? CustomHazard { get; set; }
  }

  // ViewModel untuk membuat item dalam booking
  public class BookingItemCreateViewModel
  {
    [Required(ErrorMessage = "Item name is required")]
    [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
    public string ItemName { get; set; } = string.Empty;

    [Range(0.01, 1000, ErrorMessage = "Weight must be between 0.01 and 1000")]
    public decimal Weight { get; set; }

    [Range(0.01, 1000, ErrorMessage = "Height must be between 0.01 and 1000")]
    public decimal Height { get; set; }

    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int Quantity { get; set; }
  }

  // ViewModel untuk memilih shift untuk hari tertentu
  public class DailyShiftSelectionViewModel
  {
    public DateTime Date { get; set; }
    public List<int> SelectedShiftIds { get; set; } = new List<int>();
  }

  // ViewModel untuk menampilkan booking dalam kalender
  // Di dalam BookingCalendarViewModel, tambahkan property Status
  public class BookingCalendarViewModel
  {
    public int Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    // Tambahkan property Status untuk digunakan dalam styling
    public BookingStatus Status { get; set; }
    public List<ShiftBookingViewModel> Shifts { get; set; } = new List<ShiftBookingViewModel>();
  }

  // ViewModel untuk shift dalam kalender
  public class ShiftBookingViewModel
  {
    public int ShiftDefinitionId { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
  }

  // ViewModel untuk menampilkan crane dalam kalender
  public class CraneBookingsViewModel
  {
    public string CraneId { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public List<BookingCalendarViewModel> Bookings { get; set; } = new List<BookingCalendarViewModel>();
    public List<MaintenanceCalendarViewModel> MaintenanceSchedules { get; set; } = new List<MaintenanceCalendarViewModel>();
  }

  // ViewModel untuk menampilkan maintenance schedule dalam kalender
  public class MaintenanceCalendarViewModel
  {
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<ShiftBookingViewModel> Shifts { get; set; } = new List<ShiftBookingViewModel>();
  }

  // ViewModel untuk response kalender
  public class CalendarResponseViewModel
  {
    public WeekRangeViewModel WeekRange { get; set; } = new WeekRangeViewModel();
    public List<CraneBookingsViewModel> Cranes { get; set; } = new List<CraneBookingsViewModel>();
  }

  // ViewModel untuk rentang minggu dalam kalender
  public class WeekRangeViewModel
  {
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
  }

  // ViewModel untuk menyimpan informasi shift yang telah dibooking
  // ViewModels/BookingManagement/BookingFormViewModels.cs
  public class BookedShiftViewModel
  {
    public int CraneId { get; set; }
    public DateTime Date { get; set; }
    public int? ShiftDefinitionId { get; set; } // Diubah menjadi nullable
  }
}
