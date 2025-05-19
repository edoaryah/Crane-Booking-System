using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public enum BookingStatus
  {
    PendingApproval,
    ManagerApproved,
    ManagerRejected,
    PICApproved,
    PICRejected,
    Cancelled,
    Done
  }

  public enum BookingCancelledBy
  {
    None = 0,
    User = 1,
    PIC = 2
  }

  public class Booking
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(36)]
    public string DocumentNumber { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public required string BookingNumber { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string Department { get; set; }

    [Required]
    public int CraneId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public DateTime SubmitTime { get; set; } = DateTime.Now;

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(100)]
    public string? ProjectSupervisor { get; set; }

    [StringLength(50)]
    public string? CostCode { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? CustomHazard { get; set; }

    // Status and approval properties
    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.PendingApproval;

    // Manager approval info
    public string? ManagerName { get; set; }
    public DateTime? ManagerApprovalTime { get; set; }
    [StringLength(500)]
    public string? ManagerRejectReason { get; set; }

    // PIC approval info
    public string? ApprovedByPIC { get; set; }
    public DateTime? ApprovedAtByPIC { get; set; }
    [StringLength(500)]
    public string? PICRejectReason { get; set; }

    // PIC completion info
    public string? DoneByPIC { get; set; }
    public DateTime? DoneAt { get; set; }

    // Cancellation properties
    public BookingCancelledBy CancelledBy { get; set; } = BookingCancelledBy.None;
    public string? CancelledByName { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledReason { get; set; }

    // Revision tracking
    public int RevisionCount { get; set; }
    public DateTime LastModifiedAt { get; set; } = DateTime.Now;
    public string LastModifiedBy { get; set; } = string.Empty;

    // Properti untuk penagihan
    public bool IsBilled { get; set; } = false;
    public DateTime? BilledDate { get; set; }
    public string? BilledBy { get; set; }
    public string? BillingNotes { get; set; }

    // Navigation properties
    public virtual Crane? Crane { get; set; }
    public virtual ICollection<BookingShift> BookingShifts { get; set; } = new List<BookingShift>();
    public virtual ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
    public virtual ICollection<BookingHazard> BookingHazards { get; set; } = new List<BookingHazard>();
  }
}
