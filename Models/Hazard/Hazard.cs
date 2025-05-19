using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class Hazard
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
  }
}
