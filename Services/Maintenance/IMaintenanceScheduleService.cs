using AspnetCoreMvcFull.ViewModels.MaintenanceManagement;

namespace AspnetCoreMvcFull.Services
{
  public interface IMaintenanceScheduleService
  {
    Task<IEnumerable<MaintenanceScheduleViewModel>> GetAllMaintenanceSchedulesAsync();
    Task<MaintenanceScheduleDetailViewModel> GetMaintenanceScheduleByIdAsync(int id);
    Task<MaintenanceScheduleDetailViewModel> GetMaintenanceScheduleByDocumentNumberAsync(string documentNumber);
    Task<IEnumerable<MaintenanceScheduleViewModel>> GetMaintenanceSchedulesByCraneIdAsync(int craneId);
    Task<MaintenanceScheduleDetailViewModel> CreateMaintenanceScheduleAsync(MaintenanceScheduleCreateViewModel maintenanceViewModel);
    Task<MaintenanceScheduleDetailViewModel> UpdateMaintenanceScheduleAsync(int id, MaintenanceScheduleUpdateViewModel maintenanceViewModel);
    Task DeleteMaintenanceScheduleAsync(int id);
    Task<bool> IsShiftMaintenanceConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null);
    Task<bool> MaintenanceScheduleExistsAsync(int id);
    Task<bool> MaintenanceScheduleExistsByDocumentNumberAsync(string documentNumber);
  }
}
