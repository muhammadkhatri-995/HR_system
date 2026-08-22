using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_system.Controllers
{
    // Only Admin and HR can manage employees.
    [Authorize(Roles = "Admin,HR")]
    public class EmployeeController : BaseController
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuditService _auditService;

        private readonly PasswordHasher<Employee> _passwordHasher = new();

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IRoleRepository roleRepository,
            IWebHostEnvironment webHostEnvironment,
            IAuditService auditService)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _roleRepository = roleRepository;
            _webHostEnvironment = webHostEnvironment;
            _auditService = auditService;
        }

        // GET: /Employee?searchTerm=ali&pageNumber=2
        public async Task<IActionResult> Index(
            string? searchTerm,
            int pageNumber = 1)
        {
            int pageSize = 10;

            var (employees, totalCount) =
                await _employeeRepository.GetAllAsync(
                    searchTerm,
                    pageNumber,
                    pageSize);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(employees);
        }

        // Builds Department/Role dropdowns
        private async Task PopulateDropdowns(EmployeeViewModel model)
        {
            var departments =
                await _departmentRepository.GetAllAsync();

            var roles =
                await _roleRepository.GetAllRolesAsync();

            model.Departments = departments
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToList();

            model.Roles = roles
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                })
                .ToList();
        }

        // GET: /Employee/Create
        public async Task<IActionResult> Create()
        {
            var model = new EmployeeViewModel();

            await PopulateDropdowns(model);

            return View(model);
        }

        // POST: /Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            // Password is mandatory on Create
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "Password is required");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var employee = new Employee
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                CNIC = model.CNIC,
                Address = model.Address,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                JoiningDate = model.JoiningDate,
                DepartmentId = model.DepartmentId,
                RoleId = model.RoleId,
                Salary = model.Salary,
                Status = model.Status,
                CreatedDate = DateTime.Now
            };

            // Hash password
            employee.PasswordHash =
                _passwordHasher.HashPassword(
                    employee,
                    model.Password!);

            // Save photo
            if (model.PhotoFile != null &&
                model.PhotoFile.Length > 0)
            {
                employee.EmployeePhoto =
                    await SavePhotoAsync(model.PhotoFile);
            }

            await _employeeRepository.AddAsync(employee);

            // Audit Log
            await _auditService.LogAsync(
                "Employee",
                "Create",
                $"Employee created: {employee.FirstName} {employee.LastName} (ID: {employee.id})."
            );

            NotifySuccess("Employee created successfully.");

            return RedirectToAction(nameof(Index));
        }

        // Saves employee photo
        private async Task<string> SavePhotoAsync(IFormFile file)
        {
            string uploadsFolder = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "uploads",
                "employees");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName =
                Guid.NewGuid().ToString() +
                Path.GetExtension(file.FileName);

            string filePath =
                Path.Combine(
                    uploadsFolder,
                    uniqueFileName);

            using (var fileStream =
                   new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/employees/" + uniqueFileName;
        }

        // GET: /Employee/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            var model = new EmployeeViewModel
            {
                Id = employee.id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                CNIC = employee.CNIC,
                Address = employee.Address,
                Gender = employee.Gender,
                DateOfBirth = employee.DateOfBirth,
                JoiningDate = employee.JoiningDate,
                DepartmentId = employee.DepartmentId,
                RoleId = employee.RoleId,
                Salary = employee.Salary,
                Status = employee.Status,
                ExistingPhotoPath = employee.EmployeePhoto
            };

            await PopulateDropdowns(model);

            return View(model);
        }

        // POST: /Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            EmployeeViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            employee.FirstName = model.FirstName;
            employee.LastName = model.LastName;
            employee.Email = model.Email;
            employee.Phone = model.Phone;
            employee.CNIC = model.CNIC;
            employee.Address = model.Address;
            employee.Gender = model.Gender;
            employee.DateOfBirth = model.DateOfBirth;
            employee.JoiningDate = model.JoiningDate;
            employee.DepartmentId = model.DepartmentId;
            employee.RoleId = model.RoleId;
            employee.Salary = model.Salary;
            employee.Status = model.Status;

            // Change password only when provided
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                employee.PasswordHash =
                    _passwordHasher.HashPassword(
                        employee,
                        model.Password);
            }

            // Replace photo only when a new photo is uploaded
            if (model.PhotoFile != null &&
                model.PhotoFile.Length > 0)
            {
                employee.EmployeePhoto =
                    await SavePhotoAsync(model.PhotoFile);
            }

            await _employeeRepository.UpdateAsync(employee);

            // Audit Log
            await _auditService.LogAsync(
                "Employee",
                "Update",
                $"Employee updated: {employee.FirstName} {employee.LastName} (ID: {employee.id})."
            );

            NotifySuccess("Employee updated successfully.");

            return RedirectToAction(nameof(Index));
        }

        // GET: /Employee/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: /Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Get employee BEFORE deleting so we can put useful
            // information into the audit log.
            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            string employeeName =
                $"{employee.FirstName} {employee.LastName}";

            await _employeeRepository.DeleteAsync(id);

            // Audit Log
            await _auditService.LogAsync(
                "Employee",
                "Delete",
                $"Employee deleted: {employeeName} (ID: {id})."
            );

            NotifySuccess("Employee deleted successfully.");

            return RedirectToAction(nameof(Index));
        }
    }
}