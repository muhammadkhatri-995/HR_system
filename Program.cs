using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Services;
using HR_system.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. DATA PROTECTION & KEYS PERSISTENCE
// =====================================================
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "temp-keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

// =====================================================
// 2. MVC SERVICES & ANTIFORGERY
// =====================================================
builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = ".AspNetCore.Antiforgery.KineticHR";
    options.Cookie.SameSite = SameSiteMode.Unspecified;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// =====================================================
// 3. DATABASE (PostgreSQL / Neon)
// =====================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// =====================================================
// 4. REPOSITORIES & SERVICES
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
// 5. AUDIT LOG SERVICE
// =====================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();

// =====================================================
// 6. FORWARDED HEADERS (Render / Reverse Proxy Fix)
// =====================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// =====================================================
// 7. AUTHENTICATION & COOKIE FIXES (FIXED HERE)
// =====================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Proxy fix: Unspecified and SameAsRequest allow Render SSL proxy to hold auth cookie
    options.Cookie.SameSite = SameSiteMode.Unspecified;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.Name = "KineticHRAuthCookie";
});

// =====================================================
// 8. BUILD APPLICATION
// =====================================================
var app = builder.Build();

// Enable Forwarded Headers (MUST be absolute top of pipeline)
app.UseForwardedHeaders();

app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next();
});

// =====================================================
// 9. ERROR HANDLING
// =====================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Login");
    app.UseHsts();
}

// =====================================================
// 10. STATIC FILES & ROUTING
// =====================================================
app.UseStaticFiles();
app.UseRouting();

// =====================================================
// 11. AUTHENTICATION & AUTHORIZATION
// =====================================================
app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// 12. DEFAULT ROUTE
// =====================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Automatic Database Migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// =====================================================
// 13. START APPLICATION
// =====================================================
app.Run();