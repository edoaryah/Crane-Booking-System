// Services/Dashboard/IDashboardService.cs
using AspnetCoreMvcFull.ViewModels.Dashboard;

namespace AspnetCoreMvcFull.Services.Dashboard
{
  public interface IDashboardService
  {
    Task<DashboardViewModel> GetDashboardDataAsync(string period);
  }
}
