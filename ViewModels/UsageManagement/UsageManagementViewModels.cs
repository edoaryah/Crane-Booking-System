using System.ComponentModel.DataAnnotations;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.ViewModels.UsageManagement
{
  public class UsageSubcategoryViewModel
  {
    public int Id { get; set; }

    [Display(Name = "Category")]
    public UsageCategory Category { get; set; }

    [Display(Name = "Name")]
    public required string Name { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    public string CategoryName => Category.ToString();
  }

  public class UsageSubcategoryCreateViewModel
  {
    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public UsageCategory Category { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    [Display(Name = "Name")]
    public required string Name { get; set; }

    [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
  }

  public class UsageSubcategoryUpdateViewModel
  {
    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public UsageCategory Category { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    [Display(Name = "Name")]
    public required string Name { get; set; }

    [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
  }

  public class UsageSubcategoryListViewModel
  {
    public IEnumerable<UsageSubcategoryViewModel> Subcategories { get; set; } = new List<UsageSubcategoryViewModel>();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }
}
