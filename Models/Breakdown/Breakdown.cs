// Models/Maintenance/Breakdown.cs (Updated)
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class Breakdown
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int CraneId { get; set; }

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
