// Models/CraneUsageEntry.cs
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class CraneUsageEntry
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int CraneUsageRecordId { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    public UsageCategory Category { get; set; }

    // Diubah menjadi nullable
    public int? UsageSubcategoryId { get; set; }

    // Properti baru untuk menyimpan nama subkategori secara langsung
    [StringLength(100)]
    public string? SubcategoryName { get; set; }

    public int? BookingId { get; set; }

    public int? MaintenanceScheduleId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Pindahkan OperatorName dari CraneUsageRecord ke sini
    [StringLength(100)]
    public string? OperatorName { get; set; }

    // Navigation properties
    public virtual CraneUsageRecord? CraneUsageRecord { get; set; }
    public virtual UsageSubcategory? UsageSubcategory { get; set; }
    public virtual Booking? Booking { get; set; }
    public virtual MaintenanceSchedule? MaintenanceSchedule { get; set; }
  }
}
