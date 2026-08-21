using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HR_system.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        // IHttpContextAccessor lets us reach into the CURRENT web request
        // from inside a Service — Services don't automatically have access
        // to the request the way Controllers do, so this is how we get it.
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string module, string details)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            string userName = httpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? "System";
            string? ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

            var log = new AuditLogs
            {
                UserName = userName,
                Action = action,
                Module = module,
                Details = details,
                Timestamp = DateTime.Now,
                IpAddress = ipAddress
            };

            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}