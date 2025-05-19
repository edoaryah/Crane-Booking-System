// Models/Maintenance/Breakdown.cs (Updated)
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class Breakdown
  {
    [Key]
    public int Id { get; set; }

    // Ubah CraneId menjadi nullable
    public int? CraneId { get; set; }

    // Tambahkan properti historis crane (hanya code dan capacity)
    [StringLength(50)]
    public string? CraneCode { get; set; }

    public int? CraneCapacity { get; set; }

    [Required]
    public DateTime UrgentStartTime { get; set; } = DateTime.Now;

    [Required]
    public DateTime UrgentEndTime { get; set; } // Changed from calculated to direct input

    // Kolom untuk mencatat waktu crane kembali available secara manual
    public DateTime? ActualUrgentEndTime { get; set; }

    // Kolom untuk menyimpan Hangfire JobId
    public string? HangfireJobId { get; set; }

    [Required]
    public required string Reasons { get; set; }

    public virtual Crane? Crane { get; set; }
  }
}
