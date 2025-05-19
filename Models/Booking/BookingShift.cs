using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class BookingShift
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    // Ubah ShiftDefinitionId menjadi nullable
    public int? ShiftDefinitionId { get; set; }

    // Properti tambahan untuk menyimpan data historis
    [StringLength(50)]
    public string? ShiftName { get; set; }

    public TimeSpan ShiftStartTime { get; set; }

    public TimeSpan ShiftEndTime { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual ShiftDefinition? ShiftDefinition { get; set; }
  }
}
