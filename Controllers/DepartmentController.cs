using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.AspNetCore.Mvc;
namespace HR_system.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IDepartmentRepository _departmentRepository;
        public DepartmentController(IDepartmentRepository departmentRepository)
        {
            _departmentRepository = departmentRepository;
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _departmentRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
