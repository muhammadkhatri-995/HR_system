using HR_system.Interfaces;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HR_system.Controllers
{
    [Authorize] // any logged-in employee, any role
    public class ProfileController : BaseController
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuditService _auditService;

        private readonly PasswordHasher<Models.Employee> _passwordHasher = new();

        public ProfileController(
            IEmployeeRepository employeeRepository,
            IWebHostEnvironment webHostEnvironment,
            IAuditService auditService)
        {
            _employeeRepository = employeeRepository;
            _webHostEnvironment = webHostEnvironment;
            _auditService = auditService;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // =====================================================
        // PROFILE
        // =====================================================

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var employee =
                await _employeeRepository.GetByIdAsync(
                    GetCurrentEmployeeId());

            if (employee == null)
                return NotFound();

            var model = new ProfileViewModel
            {
                Id = employee.id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentName =
                    employee.Department?.Name ?? "Unassigned",
                RoleName =
                    employee.Role?.Name ?? "Employee",
                ExistingPhotoPath =
                    employee.EmployeePhoto,
                Email = employee.Email,
                Phone = employee.Phone,
                Address = employee.Address
            };

            return View(model);
        }

        // =====================================================
        // EDIT PROFILE
        // =====================================================

        // GET: /Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var employee =
                await _employeeRepository.GetByIdAsync(
                    GetCurrentEmployeeId());

            if (employee == null)
                return NotFound();

            var model = new ProfileViewModel
            {
                Id = employee.id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentName =
                    employee.Department?.Name ?? "Unassigned",
                RoleName =
                    employee.Role?.Name ?? "Employee",
                ExistingPhotoPath =
                    employee.EmployeePhoto,
                Email = employee.Email,
                Phone = employee.Phone,
                Address = employee.Address
            };

            return View(model);
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // SECURITY:
            // Always get the employee from the logged-in user's
            // authentication claim instead of trusting model.Id.
            var employee =
                await _employeeRepository.GetByIdAsync(
                    GetCurrentEmployeeId());

            if (employee == null)
                return NotFound();

            // Only profile fields are allowed to be changed.
            employee.Email = model.Email;
            employee.Phone = model.Phone;
            employee.Address = model.Address;

            bool photoUpdated = false;

            if (model.PhotoFile != null &&
                model.PhotoFile.Length > 0)
            {
                employee.EmployeePhoto =
                    await SavePhotoAsync(model.PhotoFile);

                photoUpdated = true;
            }

            await _employeeRepository.UpdateAsync(employee);

            // =====================================================
            // AUDIT LOG
            // =====================================================

            string changes =
                $"Employee ID {employee.id} updated profile. " +
                $"Email: {employee.Email}, " +
                $"Phone: {employee.Phone}, " +
                $"Address: {employee.Address}.";

            if (photoUpdated)
            {
                changes += " Profile photo was also updated.";
            }

            await _auditService.LogAsync(
                "Profile",
                "Update",
                changes
            );

            NotifySuccess("Profile updated successfully.");

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // SAVE PHOTO
        // =====================================================

        private async Task<string> SavePhotoAsync(
            Microsoft.AspNetCore.Http.IFormFile file)
        {
            string uploadsFolder =
                Path.Combine(
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
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/employees/" +
                   uniqueFileName;
        }

        // =====================================================
        // CHANGE PASSWORD
        // =====================================================

        // GET: /Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee =
                await _employeeRepository.GetByIdAsync(
                    GetCurrentEmployeeId());

            if (employee == null)
                return NotFound();

            // Verify current password
            PasswordVerificationResult result;

            try
            {
                result =
                    _passwordHasher.VerifyHashedPassword(
                        employee,
                        employee.PasswordHash,
                        model.CurrentPassword);
            }
            catch (FormatException)
            {
                ModelState.AddModelError(
                    nameof(model.CurrentPassword),
                    "Current password is incorrect");

                return View(model);
            }

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    nameof(model.CurrentPassword),
                    "Current password is incorrect");

                return View(model);
            }

            // Create new password hash
            employee.PasswordHash =
                _passwordHasher.HashPassword(
                    employee,
                    model.NewPassword);

            await _employeeRepository.UpdateAsync(employee);

            // =====================================================
            // AUDIT LOG
            // =====================================================

            // IMPORTANT:
            // Never store CurrentPassword or NewPassword
            // inside the audit log.
            await _auditService.LogAsync(
                "Profile",
                "ChangePassword",
                $"Employee ID {employee.id} successfully changed their password."
            );

            NotifySuccess("Password changed successfully.");

            return RedirectToAction(nameof(Index));
        }
    }
}