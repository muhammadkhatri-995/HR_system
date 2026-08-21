using HR_system.Interfaces;
using HR_system.Models;
using HR_system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_system.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class DepartmentController : BaseController
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IAuditService _auditService;
        public DepartmentController(IDepartmentRepository departmentRepository , IAuditService auditService)
        {
            _departmentRepository = departmentRepository;
            _auditService = auditService;

        }
        public async Task<IActionResult> Index()
        {
            var departments = await _departmentRepository.GetAllAsync();
            return View(departments);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
          

            if (ModelState.IsValid)
            {
                await _departmentRepository.AddAsync(department);
                NotifySuccess("Department created successfully.");
                await _auditService.LogAsync("Create", "Department", $"Created department '{department.Name}'");
                return RedirectToAction(nameof(Index));

            }
            return View(department);
           

        }
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.Id)
            {
                return BadRequest();
            }
            if (ModelState.IsValid)
            {
                await _departmentRepository.UpdateAsync(department);
                NotifySuccess("Department updated   successfully.");
                await _auditService.LogAsync("Update", "Department", $"Updated department '{department.Name}'");
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, Department department)
        {
            await _departmentRepository.DeleteAsync(id);
            NotifySuccess("Department deleted successfully.");
            await _auditService.LogAsync("Delete", "Department", $"Deleted department '{department.Name}'");
            return RedirectToAction(nameof(Index));
        }
    }
}
