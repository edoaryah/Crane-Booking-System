// ViewModels/CraneManagement/CraneViewModel.cs
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Http;

namespace AspnetCoreMvcFull.ViewModels.CraneManagement
{
  public class CraneViewModel
  {
    public int Id { get; set; }
    public required string Code { get; set; }
    public int Capacity { get; set; }
    public CraneStatus Status { get; set; }
    public string? ImagePath { get; set; }
    public CraneOwnership Ownership { get; set; } = CraneOwnership.KPC; // New property
  }

  public class CraneDetailViewModel : CraneViewModel
  {
    public ICollection<BreakdownViewModel> Breakdowns { get; set; } = new List<BreakdownViewModel>();
  }

  public class CraneCreateViewModel
  {
    public required string Code { get; set; }
    public int Capacity { get; set; }
    public CraneStatus? Status { get; set; }
    public IFormFile? Image { get; set; }
    public CraneOwnership Ownership { get; set; } = CraneOwnership.KPC; // New property
  }

  public class CraneUpdateViewModel
  {
    public required string Code { get; set; }
    public int Capacity { get; set; }
    public CraneStatus? Status { get; set; }
    public IFormFile? Image { get; set; }
    public CraneOwnership Ownership { get; set; } = CraneOwnership.KPC; // New property
  }

  public class CraneUpdateWithBreakdownViewModel
  {
    public CraneUpdateViewModel Crane { get; set; } = new CraneUpdateViewModel { Code = string.Empty };
    public BreakdownCreateViewModel? Breakdown { get; set; }
  }
}
