using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class UsageSubcategory
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public UsageCategory Category { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
  }
}
