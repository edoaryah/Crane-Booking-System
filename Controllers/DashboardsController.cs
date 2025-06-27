// Controllers/DashboardsController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services.Dashboard;
using System;

namespace AspnetCoreMvcFull.Controllers;

[Authorize]
// [ServiceFilter(typeof(AuthorizationFilter))]
public class DashboardsController : Controller
{
  private readonly IDashboardService _dashboardService;
  private readonly ILogger<DashboardsController> _logger;

  public DashboardsController(IDashboardService dashboardService, ILogger<DashboardsController> logger)
  {
    _dashboardService = dashboardService;
    _logger = logger;
  }

  // [RequireRole("admin")]
  public async Task<IActionResult> Index(int? month, DateTime? startDate, DateTime? endDate)
  {
    // Tentukan periode dan parameter default
    if (!month.HasValue && !startDate.HasValue && !endDate.HasValue)
    {
      // Tidak ada filter, gunakan bulan ini sebagai default
      month = DateTime.Now.Month;
    }

    string period;
    if (month.HasValue)
    {
      period = "by_month";
    }
    else if (startDate.HasValue && endDate.HasValue)
    {
      period = "custom";
    }
    else
    {
      period = "month"; // Default to current month
    }

    try
    {
      var viewModel = await _dashboardService.GetDashboardDataAsync(period, month, startDate, endDate);
      return View(viewModel);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error loading dashboard data for period {Period}", period);
      var errorViewModel = new ViewModels.Dashboard.DashboardViewModel
      {
        SelectedPeriod = period,
        SelectedMonth = month,
        StartDate = startDate,
        EndDate = endDate
      };
      return View(errorViewModel);
    }
  }
}
