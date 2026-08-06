using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_system.Controllers
{
    // Only Admin and HR can manage employees — a plain "Employee" role
    // should not be able to create/edit/delete other employees.
    [Authorize(Roles = "Admin,HR")]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // PasswordHasher<Employee> is the same built-in hasher used in AccountController
        // for login verification. Using the SAME generic type (<Employee>) here
        // ensures consistency between how passwords are hashed and how they're checked.
        private readonly PasswordHasher<Employee> _passwordHasher = new();

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IRoleRepository roleRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _roleRepository = roleRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Employee?searchTerm=ali&pageNumber=2
        public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
        {
            int pageSize = 10;

            var (employees, totalCount) = await _employeeRepository.GetAllAsync(searchTerm, pageNumber, pageSize);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(employees);
        }

        // Builds the Department/Role dropdown option lists for the Create/Edit forms
        private async Task PopulateDropdowns(EmployeeViewModel model)
        {
            var departments = await _departmentRepository.GetAllAsync();
            var roles = await _roleRepository.GetAllRolesAsync();

            model.Departments = departments
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToList();

            model.Roles = roles
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
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
            // On CREATE, a password is mandatory. The ViewModel itself keeps
            // Password as optional (string?) because Edit reuses the same class
            // without requiring a new password — so we enforce "required on Create"
            // manually here instead of with a [Required] attribute.
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Password is required");
            }

            if (!ModelState.IsValid)
            {
                // Dropdown lists are NOT part of the submitted form data,
                // so they're always empty on postback — we must refill them
                // before returning the view, or the dropdowns will render blank.
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

            // HashPassword takes the plain-text password typed by the admin and
            // returns a one-way, salted hash. The "employee" object passed first
            // isn't read from — it's just required by the method signature for
            // internal type consistency. The plain password is NEVER stored anywhere.
            employee.PasswordHash = _passwordHasher.HashPassword(employee, model.Password!);

            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                employee.EmployeePhoto = await SavePhotoAsync(model.PhotoFile);
            }

            await _employeeRepository.AddAsync(employee);
            return RedirectToAction(nameof(Index));
        }

        // Saves the uploaded photo to wwwroot/uploads/employees and returns
        // the web-accessible relative path to store in the database.
        private async Task<string> SavePhotoAsync(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "employees");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/employees/" + uniqueFileName;
        }

        // GET: /Employee/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
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
                // Password / ConfirmPassword deliberately left null/empty here —
                // we NEVER send an existing password hash back into a form field.
                // Leaving them blank on the Edit screen means "keep current password."
            };

            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: /Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // On EDIT, password is OPTIONAL. If the admin leaves both Password
            // and ConfirmPassword blank, [Compare] passes trivially (both null),
            // and ModelState.IsValid stays true — meaning "no password change requested."
            // If they type something in Password but it's under 6 chars or doesn't
            // match ConfirmPassword, validation still catches that correctly.
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var employee = await _employeeRepository.GetByIdAsync(id);
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

            // Only re-hash and overwrite PasswordHash if the admin actually typed
            // a new password. Otherwise, the employee's existing password stays valid.
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                employee.PasswordHash = _passwordHasher.HashPassword(employee, model.Password);
            }

            // Only replace the photo if a new one was uploaded —
            // otherwise keep the existing EmployeePhoto path untouched.
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                employee.EmployeePhoto = await SavePhotoAsync(model.PhotoFile);
            }

            await _employeeRepository.UpdateAsync(employee);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Employee/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
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
            await _employeeRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}