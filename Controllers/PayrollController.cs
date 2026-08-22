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
    public class PayrollController : BaseController
    {
        private readonly IPayrollRepository _payrollRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAuditService _auditService;

        public PayrollController(
            IPayrollRepository payrollRepository,
            IEmployeeRepository employeeRepository,
            IAuditService auditService)
        {
            _payrollRepository = payrollRepository;
            _employeeRepository = employeeRepository;
            _auditService = auditService;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // GET: /Payroll
        // Admin/HR see everyone's payroll history.
        // Employee only sees their own payroll records.
        public async Task<IActionResult> Index(int? employeeId)
        {
            bool isManager = User.IsInRole("Admin") || User.IsInRole("HR");
            ViewBag.IsManager = isManager;

            if (isManager)
            {
                var records =
                    await _payrollRepository.GetAllAsync(employeeId);

                var (employees, _) =
                    await _employeeRepository.GetAllAsync(
                        null,
                        1,
                        1000);

                ViewBag.Employees = employees
                    .Select(e => new SelectListItem
                    {
                        Value = e.id.ToString(),
                        Text = $"{e.FirstName} {e.LastName}"
                    })
                    .ToList();

                ViewBag.SelectedEmployeeId = employeeId;

                return View(records);
            }

            var myRecords =
                await _payrollRepository.GetAllAsync(
                    GetCurrentEmployeeId());

            return View(myRecords);
        }

        // GET: /Payroll/Create
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            var model = new PayrollViewModel();

            await PopulateEmployeeDropdown(model);

            return View(model);
        }

        private async Task PopulateEmployeeDropdown(
            PayrollViewModel model)
        {
            var (employees, _) =
                await _employeeRepository.GetAllAsync(
                    null,
                    1,
                    1000);

            model.Employees = employees
                .Select(e => new SelectListItem
                {
                    Value = e.id.ToString(),
                    Text = $"{e.FirstName} {e.LastName}"
                })
                .ToList();
        }

        // POST: /Payroll/Create
        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PayrollViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown(model);
                return View(model);
            }

            // Get the current employee so we can snapshot
            // their current salary.
            var employee =
                await _employeeRepository.GetByIdAsync(
                    model.EmployeeId);

            if (employee == null)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeId),
                    "Selected employee not found");

                await PopulateEmployeeDropdown(model);

                return View(model);
            }

            var payroll = new PayRoll
            {
                EmployeeId = model.EmployeeId,

                // Snapshot employee's salary at processing time
                BasicSalary = employee.Salary,

                Bonus = model.Bonus,
                Deduction = model.Deduction,
                PayDate = model.PayDate,

                // Net = Basic Salary + Bonus - Deduction
                NetSalary =
                    employee.Salary +
                    model.Bonus -
                    model.Deduction
            };

            await _payrollRepository.AddAsync(payroll);

            // =====================================================
            // AUDIT LOG
            // =====================================================

            await _auditService.LogAsync(
                "Payroll",
                "Create",
                $"Payroll record created for Employee ID {employee.id} " +
                $"({employee.FirstName} {employee.LastName}) " +
                $"for {payroll.PayDate:dd-MM-yyyy}. " +
                $"Basic Salary: {payroll.BasicSalary}, " +
                $"Bonus: {payroll.Bonus}, " +
                $"Deduction: {payroll.Deduction}, " +
                $"Net Salary: {payroll.NetSalary}."
            );

            NotifySuccess(
                "Payroll record created successfully.");

            return RedirectToAction(nameof(Index));
        }

        // GET: /Payroll/Delete/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var payroll =
                await _payrollRepository.GetByIdAsync(id);

            if (payroll == null)
                return NotFound();

            return View(payroll);
        }

        // POST: /Payroll/Delete/5
        [Authorize(Roles = "Admin,HR")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Get the payroll BEFORE deleting it.
            // This allows us to put useful information
            // into the audit log.
            var payroll =
                await _payrollRepository.GetByIdAsync(id);

            if (payroll == null)
                return NotFound();

            await _payrollRepository.DeleteAsync(id);

            // =====================================================
            // AUDIT LOG
            // =====================================================

            await _auditService.LogAsync(
                "Payroll",
                "Delete",
                $"Payroll record ID {id} deleted for " +
                $"Employee ID {payroll.EmployeeId}. " +
                $"Pay Date: {payroll.PayDate:dd-MM-yyyy}, " +
                $"Net Salary: {payroll.NetSalary}."
            );

            NotifySuccess(
                "Payroll record deleted successfully.");

            return RedirectToAction(nameof(Index));
        }
    }
}