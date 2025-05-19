// Controllers/BreakdownHistoryController.cs (Updated)
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class BreakdownHistoryController : Controller
  {
    private readonly ICraneService _craneService;

    public BreakdownHistoryController(ICraneService craneService)
    {
      _craneService = craneService;
    }

    public async Task<IActionResult> Index(int? craneId = null)
    {
      // Get all breakdowns
      var breakdowns = await _craneService.GetAllBreakdownsAsync();

      // Filter by craneId if provided
      if (craneId.HasValue)
      {
        breakdowns = breakdowns.Where(b => b.CraneId == craneId.Value).ToList();
        ViewData["CraneId"] = craneId.Value;
      }

      return View(breakdowns);
    }
  }
}
