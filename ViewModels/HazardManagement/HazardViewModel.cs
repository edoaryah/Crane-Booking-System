// ViewModels/HazardManagement/HazardViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.HazardManagement
{
  public class HazardViewModel
  {
    public int Id { get; set; }

    [Display(Name = "Hazard Name")]
    public required string Name { get; set; }
  }

  public class HazardCreateViewModel
  {
    [Required(ErrorMessage = "Hazard name is required")]
    [StringLength(100, ErrorMessage = "Hazard name cannot exceed 100 characters")]
    [Display(Name = "Hazard Name")]
    public required string Name { get; set; }
  }

  public class HazardUpdateViewModel
  {
    [Required(ErrorMessage = "Hazard name is required")]
    [StringLength(100, ErrorMessage = "Hazard name cannot exceed 100 characters")]
    [Display(Name = "Hazard Name")]
    public required string Name { get; set; }
  }

  public class HazardListViewModel
  {
    public IEnumerable<HazardViewModel> Hazards { get; set; } = new List<HazardViewModel>();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
  }
}
