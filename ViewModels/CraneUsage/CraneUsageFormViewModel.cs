using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.CraneUsage
{
  public class CraneUsageFormViewModel
  {
    public int CraneId { get; set; }
    public string CraneCode { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;

    // Finalization properties
    public bool IsFinalized { get; set; }
    public string? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }

    // List of time entries
    public List<CraneUsageEntryViewModel> Entries { get; set; } = new List<CraneUsageEntryViewModel>();
  }
}
