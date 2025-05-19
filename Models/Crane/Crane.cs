// Models/Crane/Crane.cs (Updated)
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public enum CraneStatus
  {
    Available,
    Maintenance
  }

  public enum CraneOwnership
  {
    KPC,
    Kontraktor
  }

  public class Crane
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Code { get; set; }

    [Required]
    public int Capacity { get; set; }

    [Required]
    public CraneStatus Status { get; set; } = CraneStatus.Available;

    [Required]
    public CraneOwnership Ownership { get; set; } = CraneOwnership.KPC; // New property

    public string? ImagePath { get; set; }

    public ICollection<Breakdown> Breakdowns { get; set; } = new List<Breakdown>();
  }
}
