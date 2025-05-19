using AspnetCoreMvcFull.ViewModels.ShiftManagement;

namespace AspnetCoreMvcFull.Services
{
  public interface IShiftDefinitionService
  {
    Task<IEnumerable<ShiftViewModel>> GetAllShiftDefinitionsAsync();
    Task<ShiftViewModel> GetShiftDefinitionByIdAsync(int id);
    Task<ShiftViewModel> CreateShiftDefinitionAsync(ShiftCreateViewModel shiftViewModel);
    Task<ShiftViewModel> UpdateShiftDefinitionAsync(int id, ShiftUpdateViewModel shiftViewModel);
    Task DeleteShiftDefinitionAsync(int id);
    Task<bool> ShiftDefinitionExistsAsync(int id);
  }
}
