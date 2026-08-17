using HR_system.Interfaces;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HR_system.Controllers
{
    [Authorize] // any logged-in employee, any role
    public class ProfileController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly PasswordHasher<Models.Employee> _passwordHasher = new();

        public ProfileController(
            IEmployeeRepository employeeRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _employeeRepository = employeeRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var employee = await _employeeRepository.GetByIdAsync(GetCurrentEmployeeId());
            if (employee == null) return NotFound();

            var model = new ProfileViewModel
            {
                Id = employee.id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentName = employee.Department?.Name ?? "Unassigned",
                RoleName = employee.Role?.Name ?? "Employee",
                ExistingPhotoPath = employee.EmployeePhoto,
                Email = employee.Email,
                Phone = employee.Phone,
                Address = employee.Address
            };

            return View(model);
        }

        // GET: /Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var employee = await _employeeRepository.GetByIdAsync(GetCurrentEmployeeId());
            if (employee == null) return NotFound();

            var model = new ProfileViewModel
            {
                Id = employee.id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentName = employee.Department?.Name ?? "Unassigned",
                RoleName = employee.Role?.Name ?? "Employee",
                ExistingPhotoPath = employee.EmployeePhoto,
                Email = employee.Email,
                Phone = employee.Phone,
                Address = employee.Address
            };

            return View(model);
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // SECURITY: always re-fetch the REAL employee record from the
            // database using the ID from the logged-in user's cookie —
            // never trust model.Id from the submitted form. Otherwise a
            // malicious user could edit model.Id in the browser dev tools
            // and update SOMEONE ELSE's profile instead of their own.
            var employee = await _employeeRepository.GetByIdAsync(GetCurrentEmployeeId());
            if (employee == null) return NotFound();

            // Only update the fields this form is actually allowed to touch.
            // Salary, DepartmentId, RoleId, Status are NEVER assigned here —
            // they simply don't exist on ProfileViewModel, so there's no way
            // for this code to accidentally change them.
            employee.Email = model.Email;
            employee.Phone = model.Phone;
            employee.Address = model.Address;

            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                employee.EmployeePhoto = await SavePhotoAsync(model.PhotoFile);
            }

            await _employeeRepository.UpdateAsync(employee);

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SavePhotoAsync(Microsoft.AspNetCore.Http.IFormFile file)
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

        // GET: /Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee = await _employeeRepository.GetByIdAsync(GetCurrentEmployeeId());
            if (employee == null) return NotFound();

            // Verify they actually know their CURRENT password before allowing the change.
            PasswordVerificationResult result;
            try
            {
                result = _passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, model.CurrentPassword);
            }
            catch (FormatException)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect");
                return View(model);
            }

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect");
                return View(model);
            }

            employee.PasswordHash = _passwordHasher.HashPassword(employee, model.NewPassword);
            await _employeeRepository.UpdateAsync(employee);

            TempData["SuccessMessage"] = "Password changed successfully. Please log in again next time with your new password.";
            return RedirectToAction(nameof(Index));
        }
    }
}