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
    public class AccountController : BaseController
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly PasswordHasher<Models.Employee> _passwordHasher = new();

        public AccountController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // FIX: Agar user already login hai to dobara Login page na dikhao, direct Dashboard par redirect kar do
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR"))
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                return RedirectToAction("MyDashboard", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken] // Render reverse-proxy Bad Request 400 fix
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the employee by email
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

            // Build identity claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.id.ToString()),
                new Claim(ClaimTypes.Name, $"{employee.FirstName} {employee.LastName}"),
                new Claim(ClaimTypes.Email, employee.Email),
                new Claim(ClaimTypes.Role, employee.Role?.Name ?? "Employee")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Persistent properties for proxy cookies
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            // Write cookie to response
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // ReturnUrl redirect handling (Fixes loop)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Redirect based on Role
            if (employee.Role?.Name == "Admin" || employee.Role?.Name == "HR")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return RedirectToAction("MyDashboard", "Dashboard");
        }

        // POST: /Account/Logout
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            NotifySuccess("Logged out successfully.");
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}