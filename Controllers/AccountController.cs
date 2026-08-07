using HR_system.Interfaces;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HR_system.Controllers
{
    public class AccountController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly PasswordHasher<Models.Employee> _passwordHasher = new();

        public AccountController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the employee by email.
            var employee = await _employeeRepository.GetEmployeeByEmailAsync(model.Email);

            if (employee == null)
            {
                // Deliberately vague error message — never reveal whether
                // the EMAIL or the PASSWORD was wrong. This prevents
                // attackers from "probing" which emails exist in the system.
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            // VerifyHashedPassword compares the plain-text password the user typed
            // against the stored hash. We wrap it in try/catch because if this
            // particular employee's PasswordHash was never properly hashed
            // (e.g. blank, plain text, or set directly in the database),
            // Convert.FromBase64String() throws a FormatException instead of
            // just returning "Failed" — we want to treat both cases the same way.
            PasswordVerificationResult result;

            try
            {
                result = _passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, model.Password);
            }
            catch (FormatException)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            // ----- Build the identity (claims) for this logged-in user -----

            // Claims are little pieces of information about the user that get
            // packed into the encrypted login cookie. We can read these back
            // on every page via User.Identity / User.Claims, without hitting the database again.
            var claims = new List<Claim>
            {
                // FIX: "Id" with a capital I — matches the Employee model's property name exactly.
                new Claim(ClaimTypes.NameIdentifier, employee.id.ToString()),
                new Claim(ClaimTypes.Name, $"{employee.FirstName} {employee.LastName}"),
                new Claim(ClaimTypes.Email, employee.Email),
                // employee.Role comes from .Include(e => e.Role) in the Repository.
                // This Role claim is what [Authorize(Roles = "Admin")] checks later.
                new Claim(ClaimTypes.Role, employee.Role?.Name ?? "Employee")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // This actually writes the encrypted cookie to the browser.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Dashboard");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Removes the login cookie, ending the session.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}