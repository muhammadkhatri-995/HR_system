using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace HR_system.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IWebHostEnvironment _webHostEnvironment; //// needed to find wwwroot's real disk path

        // constructor injection of the repositories and web host environment
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
        public async Task<IActionResult> Index(string? searchTerm, int pageNumber = 1)
        {
            int pageSize = 10; // show 10 employees per page

            var (employees, totalCount) = await _employeeRepository.GetAllAsync(searchTerm, pageNumber, pageSize);

            // We pass paging info to the View using ViewBag —
            // a quick way to send small extra values without creating a ViewModel for it.
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(employees);
        }
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
        public async Task<IActionResult> Create()
        {
            var model = new EmployeeViewModel();
            await PopulateDropdowns(model);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model); // must re-populate — dropdowns are lost on postback
                return View(model);
            }

            // Map the ViewModel fields into a real Employee entity
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

            // Handle photo upload, if one was provided
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                employee.EmployeePhoto = await SavePhotoAsync(model.PhotoFile);
            }

            await _employeeRepository.AddAsync(employee);
            return RedirectToAction(nameof(Index));
        }

        // Handles saving an uploaded photo to wwwroot/uploads/employees
        // and returns the relative path to store in the database.
        private async Task<string> SavePhotoAsync(IFormFile file)
        {
            // wwwroot is the ONLY folder that's publicly accessible via URL,
            // so uploaded files that need to be shown in <img> tags must go here.
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "employees");

            // Create the folder if it doesn't exist yet (first upload ever)
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate a unique file name so two employees uploading "photo.jpg"
            // don't overwrite each other's files.
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Copy the uploaded file's bytes to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Store only the WEB-ACCESSIBLE relative path in the database,
            // e.g. "/uploads/employees/abc123.jpg" — this is what <img src="..."> will use.
            return "/uploads/employees/" + uniqueFileName;
        }
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Map the Employee entity back into a ViewModel for the form
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
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

            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Update every field from the ViewModel
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

            // Only replace the photo if the user actually uploaded a new one —
            // otherwise, keep the existing photo path untouched.
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
