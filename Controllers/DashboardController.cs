using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HR_system.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAuditService _auditService;

        public DashboardController(
            IEmployeeRepository employeeRepository,
            IAuditService auditService)
        {
            _employeeRepository = employeeRepository;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            // Get dashboard data
            var dashboardData = await _employeeRepository.GetDashboardDataAsync();

            // Create audit log
            await _auditService.LogAsync(
                "Dashboard",
                "Viewed",
                "User opened the dashboard"
            );

            return View(dashboardData);
        }
    }
}