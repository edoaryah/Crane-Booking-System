// Controllers/DashboardsController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services.Dashboard;

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
  public async Task<IActionResult> Index(string period = "month")
  {
    try
    {
      var viewModel = await _dashboardService.GetDashboardDataAsync(period);
      return View(viewModel);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error loading dashboard data");
      return View(new ViewModels.Dashboard.DashboardViewModel());
    }
  }
}
