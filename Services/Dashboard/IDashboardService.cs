// Services/Dashboard/IDashboardService.cs
using AspnetCoreMvcFull.ViewModels.Dashboard;
using System;

namespace AspnetCoreMvcFull.Services.Dashboard
{
  public interface IDashboardService
  {
    Task<DashboardViewModel> GetDashboardDataAsync(string period, int? month, DateTime? startDate, DateTime? endDate);
  }
}
