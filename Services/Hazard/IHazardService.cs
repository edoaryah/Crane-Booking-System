using AspnetCoreMvcFull.ViewModels.HazardManagement;

namespace AspnetCoreMvcFull.Services
{
  public interface IHazardService
  {
    Task<IEnumerable<HazardViewModel>> GetAllHazardsAsync();
    Task<HazardViewModel> GetHazardByIdAsync(int id);
    Task<HazardViewModel> CreateHazardAsync(HazardCreateViewModel hazardViewModel);
    Task<HazardViewModel> UpdateHazardAsync(int id, HazardUpdateViewModel hazardViewModel);
    Task DeleteHazardAsync(int id);
    Task<bool> HazardExistsAsync(int id);
  }
}
