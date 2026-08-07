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
        public DashboardController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }
        public async Task<IActionResult> Index()
        {
            var dashboardData = await _employeeRepository.GetDashboardDataAsync();
            return View(dashboardData);
        }






    }
}
