using HR_system.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HR_system.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        public async Task<IActionResult> Index()
        {
            bool isManager = User.IsInRole("Admin") || User.IsInRole("HR");

            if (isManager)
            {
                var fullData = await _dashboardService.GetDashboardDataAsync(GetCurrentEmployeeId());
                return View("Index", fullData);
            }

            // A plain Employee gets a completely different View and a
            // completely different, narrower ViewModel — they physically
            // cannot receive company-wide data through this code path.
            var myData = await _dashboardService.GetMyDashboardDataAsync(GetCurrentEmployeeId());
            return View("MyDashboard", myData);
        }
    }
}