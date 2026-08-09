using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IAttendanceRequestRepository, AttendanceRequestRepository>();

// ----- AUTHENTICATION SETUP (new) -----

// This registers Cookie Authentication as the default scheme.
// "CookieAuthenticationDefaults.AuthenticationScheme" is just a constant
// string ("Cookies") — using the constant avoids typos.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // If a user isn't logged in and tries to access a protected page,
        // they get redirected here instead of seeing a blank 401 error.
        options.LoginPath = "/Account/Login";

        // If a logged-in user tries to access a page their role doesn't allow,
        // they land here instead of a blank 403 error.
        options.AccessDeniedPath = "/Account/AccessDenied";

        // How long the login cookie stays valid before requiring re-login.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true; // resets the 8-hour timer on activity
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Login");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: UseAuthentication() MUST come before UseAuthorization().
// Authentication figures out WHO the user is (reads the cookie).
// Authorization then decides WHAT that user is allowed to do.
// Reversing this order breaks role checks.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();