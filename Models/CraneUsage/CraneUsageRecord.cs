using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class CraneUsageRecord
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int CraneId { get; set; }

    [Required]
    public DateTime Date { get; set; } // Hanya menyimpan tanggal

    // New properties for finalization
    public bool IsFinalized { get; set; } = false;

    [StringLength(100)]
    public string? FinalizedBy { get; set; }

    public DateTime? FinalizedAt { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(100)]
    public required string CreatedBy { get; set; }

    // Navigation properties
    public virtual Crane? Crane { get; set; }
    public virtual ICollection<CraneUsageEntry> Entries { get; set; } = new List<CraneUsageEntry>();
  }
}
