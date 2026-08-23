//using HR_system.Data;
//using HR_system.Interfaces;
//using HR_system.Services;
//using HR_system.Repositories;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.EntityFrameworkCore;
//QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllersWithViews();

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
//builder.Services.AddScoped<IRoleRepository, RoleRepository>();
//builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
//builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
//builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
//builder.Services.AddScoped<IAttendanceRequestRepository, AttendanceRequestRepository>();
//builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();    
//builder.Services.AddScoped<IAuditService, AuditService>();

//// ----- AUTHENTICATION SETUP (new) -----

//// This registers Cookie Authentication as the default scheme.
//// "CookieAuthenticationDefaults.AuthenticationScheme" is just a constant
//// string ("Cookies") — using the constant avoids typos.
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        // If a user isn't logged in and tries to access a protected page,
//        // they get redirected here instead of seeing a blank 401 error.
//        options.LoginPath = "/Account/Login";

//        // If a logged-in user tries to access a page their role doesn't allow,
//        // they land here instead of a blank 403 error.
//        options.AccessDeniedPath = "/Account/AccessDenied";

//        // How long the login cookie stays valid before requiring re-login.
//        options.ExpireTimeSpan = TimeSpan.FromHours(8);
//        options.SlidingExpiration = true; // resets the 8-hour timer on activity
//    });

//var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Account/Login");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//// IMPORTANT: UseAuthentication() MUST come before UseAuthorization().
//// Authentication figures out WHO the user is (reads the cookie).
//// Authorization then decides WHAT that user is allowed to do.
//// Reversing this order breaks role checks.
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Account}/{action=Login}/{id?}");

//app.Run();
using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Services;
using HR_system.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;


QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. MVC SERVICES
// =====================================================

builder.Services.AddControllersWithViews();


// =====================================================
// 2. DATABASE
// =====================================================

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")
//    ));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// =====================================================
// 3. REPOSITORIES
// =====================================================

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IAttendanceRequestRepository, AttendanceRequestRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();


// =====================================================
// 4. AUDIT LOG SERVICE
// =====================================================

// Allows AuditService to access the current HttpContext.
// This is required because AuditService uses IHttpContextAccessor.
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuditService, AuditService>();


// =====================================================
// 5. AUTHENTICATION
// =====================================================

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
)
.AddCookie(options =>
{
    // User is redirected here if they are NOT logged in.
    options.LoginPath = "/Account/Login";

    // User is redirected here if they are logged in
    // but don't have permission for a page.
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Login cookie remains valid for 8 hours.
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    // User activity refreshes the cookie expiration time.
    options.SlidingExpiration = true;
});


// =====================================================
// 6. BUILD APPLICATION
// =====================================================

var app = builder.Build();


// =====================================================
// 7. ERROR HANDLING
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Login");
    app.UseHsts();
}


// =====================================================
// 8. HTTPS
// =====================================================

app.UseHttpsRedirection();


// =====================================================
// 9. STATIC FILES
// =====================================================

app.UseStaticFiles();


// =====================================================
// 10. ROUTING
// =====================================================

app.UseRouting();


// =====================================================
// 11. AUTHENTICATION & AUTHORIZATION
// =====================================================

// Authentication identifies the logged-in user.
app.UseAuthentication();

// Authorization checks whether that user has permission.
app.UseAuthorization();


// =====================================================
// 12. DEFAULT ROUTE
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);


// =====================================================
// 13. START APPLICATION
// =====================================================

app.Run();