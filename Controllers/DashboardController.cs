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

        private int? GetCurrentEmployeeId()
        {
            // NameIdentifier, 'sub', aur 'id' sab check karein taake proxy claim drop na ho
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue("sub")
                       ?? User.FindFirstValue("id");

            if (int.TryParse(idClaim, out int employeeId))
            {
                return employeeId;
            }

            return null;
        }

        public async Task<IActionResult> Index()
        {
            var empId = GetCurrentEmployeeId();

            // Agar identity claim lose ho gaya ho, to safely login par bhej dein crash hone ke bajaye
            if (!empId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            bool isManager = User.IsInRole("Admin") || User.IsInRole("HR");

            if (isManager)
            {
                var fullData = await _dashboardService.GetDashboardDataAsync(empId.Value);
                return View("Index", fullData);
            }

            var myData = await _dashboardService.GetMyDashboardDataAsync(empId.Value);
            return View("MyDashboard", myData);
        }
    }
}