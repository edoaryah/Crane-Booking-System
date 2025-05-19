using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class BookingHazard
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    // Ubah menjadi nullable
    public int? HazardId { get; set; }

    // Tambah properti untuk menyimpan nama hazard
    [StringLength(100)]
    public string? HazardName { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Hazard? Hazard { get; set; }
  }
}
