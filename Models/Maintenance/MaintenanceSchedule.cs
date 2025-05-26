using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class MaintenanceSchedule
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(36)]
    public string DocumentNumber { get; set; } = Guid.NewGuid().ToString();

    // Ubah CraneId menjadi nullable
    public int? CraneId { get; set; }

    // Tambahkan properti data historis Crane
    [StringLength(50)]
    public string? CraneCode { get; set; }

    public int? CraneCapacity { get; set; }

    [Required]
    public required string Title { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public required string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    public virtual Crane? Crane { get; set; }

    public virtual ICollection<MaintenanceScheduleShift> MaintenanceScheduleShifts { get; set; } = new List<MaintenanceScheduleShift>();
  }
}
