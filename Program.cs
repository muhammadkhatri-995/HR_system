using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Services;
using HR_system.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. MVC SERVICES & ANTIFORGERY
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();

// 2. DATABASE (PostgreSQL / Neon)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. REPOSITORIES & SERVICES
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IAttendanceRequestRepository, AttendanceRequestRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// 4. AUDIT LOG SERVICE
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();

// 5. FORWARDED HEADERS (Render / Reverse Proxy Fix)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 6. AUTHENTICATION & COOKIE FIXES
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name = "KineticHRAuthCookie";
});

// 7. BUILD APPLICATION
var app = builder.Build();

app.UseForwardedHeaders();

app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next();
});

// 8. ERROR HANDLING
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Login");
    app.UseHsts();
}

// 9. STATIC FILES & ROUTING
app.UseStaticFiles();
app.UseRouting();

// 10. AUTHENTICATION & AUTHORIZATION
app.UseAuthentication();
app.UseAuthorization();

// 11. DEFAULT ROUTE
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// 12. SAFE DATABASE MIGRATION (Prevents Startup Shutdown Crash)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Migration Warning: {ex.Message}");
}

// 13. START APPLICATION
app.Run();