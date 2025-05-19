// Services/CraneManagement/ICraneService.cs (Updated)
using AspnetCoreMvcFull.ViewModels.CraneManagement;

namespace AspnetCoreMvcFull.Services
{
  public interface ICraneService
  {
    Task<IEnumerable<CraneViewModel>> GetAllCranesAsync();
    Task<CraneDetailViewModel> GetCraneByIdAsync(int id);
    Task<IEnumerable<BreakdownViewModel>> GetCraneBreakdownsAsync(int id);
    Task<CraneViewModel> CreateCraneAsync(CraneCreateViewModel craneViewModel);
    Task UpdateCraneAsync(int id, CraneUpdateWithBreakdownViewModel updateViewModel);
    Task DeleteCraneAsync(int id);
    Task ChangeCraneStatusToAvailableAsync(int craneId);
    Task<bool> CraneExistsAsync(int id);

    // Update image methods
    Task<bool> UpdateCraneImageAsync(int id, IFormFile image);
    Task RemoveCraneImageAsync(int id);

    // Breakdown history
    Task<IEnumerable<BreakdownHistoryViewModel>> GetAllBreakdownsAsync();
  }
}
