using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace HR_system.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        private readonly IPayrollRepository _payrollRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public PayrollController(
            IPayrollRepository payrollRepository,
            IEmployeeRepository employeeRepository)
        {
            _payrollRepository = payrollRepository;
            _employeeRepository = employeeRepository;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // GET: /Payroll — Admin/HR see everyone's payroll history,
        // a plain Employee only sees their own (e.g. viewing their own payslips).
        public async Task<IActionResult> Index(int? employeeId)
        {
            bool isManager = User.IsInRole("Admin") || User.IsInRole("HR");
            ViewBag.IsManager = isManager;

            if (isManager)
            {
                var records = await _payrollRepository.GetAllAsync(employeeId);

                var (employees, _) = await _employeeRepository.GetAllAsync(null, 1, 1000);
                ViewBag.Employees = employees
                    .Select(e => new SelectListItem { Value = e.id.ToString(), Text = $"{e.FirstName} {e.LastName}" })
                    .ToList();
                ViewBag.SelectedEmployeeId = employeeId;

                return View(records);
            }

            var myRecords = await _payrollRepository.GetAllAsync(GetCurrentEmployeeId());
            return View(myRecords);
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            var model = new PayrollViewModel();
            await PopulateEmployeeDropdown(model);
            return View(model);
        }

        private async Task PopulateEmployeeDropdown(PayrollViewModel model)
        {
            var (employees, _) = await _employeeRepository.GetAllAsync(null, 1, 1000);
            model.Employees = employees
                .Select(e => new SelectListItem { Value = e.id.ToString(), Text = $"{e.FirstName} {e.LastName}" })
                .ToList();
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PayrollViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown(model);
                return View(model);
            }

            // Look up the real, current Employee to snapshot their salary.
            var employee = await _employeeRepository.GetByIdAsync(model.EmployeeId);
            if (employee == null)
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "Selected employee not found");
                await PopulateEmployeeDropdown(model);
                return View(model);
            }

            var payroll = new PayRoll
            {
                EmployeeId = model.EmployeeId,
                BasicSalary = employee.Salary,     // snapshot taken HERE, at processing time
                Bonus = model.Bonus,
                Deduction = model.Deduction,
                PayDate = model.PayDate,
                // The core payroll formula: Net = Basic + Bonus - Deduction
                NetSalary = employee.Salary + model.Bonus - model.Deduction
            };

            await _payrollRepository.AddAsync(payroll);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var payroll = await _payrollRepository.GetByIdAsync(id);
            if (payroll == null) return NotFound();
            return View(payroll);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _payrollRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}