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
        [IgnoreAntiforgeryToken] // <-- Render reverse-proxy Bad Request 400 fix
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
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

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

            // Build the identity (claims) for logged-in user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.id.ToString()),
                new Claim(ClaimTypes.Name, $"{employee.FirstName} {employee.LastName}"),
                new Claim(ClaimTypes.Email, employee.Email),
                new Claim(ClaimTypes.Role, employee.Role?.Name ?? "Employee")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Dashboard");
        }

        // POST: /Account/Logout
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
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