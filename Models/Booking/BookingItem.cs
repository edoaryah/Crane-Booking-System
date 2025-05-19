using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspnetCoreMvcFull.Models
{
  public class BookingItem
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    [Required]
    [StringLength(100)]
    public required string ItemName { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Weight { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Height { get; set; }

    [Required]
    public int Quantity { get; set; }

    public virtual Booking? Booking { get; set; }
  }
}
