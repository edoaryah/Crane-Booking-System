using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class ShiftDefinition
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public required string Name { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public virtual ICollection<BookingShift> BookingShifts { get; set; } = new List<BookingShift>();
  }
}
