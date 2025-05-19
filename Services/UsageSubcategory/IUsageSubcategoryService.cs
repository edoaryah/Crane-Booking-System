using AspnetCoreMvcFull.ViewModels.UsageManagement;

namespace AspnetCoreMvcFull.Services
{
  public interface IUsageSubcategoryService
  {
    Task<IEnumerable<UsageSubcategoryViewModel>> GetAllUsageSubcategoriesAsync();
    Task<UsageSubcategoryViewModel> GetUsageSubcategoryByIdAsync(int id);
    Task<UsageSubcategoryViewModel> CreateUsageSubcategoryAsync(UsageSubcategoryCreateViewModel viewModel);
    Task<UsageSubcategoryViewModel> UpdateUsageSubcategoryAsync(int id, UsageSubcategoryUpdateViewModel viewModel);
    Task DeleteUsageSubcategoryAsync(int id);
  }
}
