using HR_system.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? module, [FromQuery(Name = "action")] string? actionFilter)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(module))
                query = query.Where(a => a.Module == module);

            if (!string.IsNullOrWhiteSpace(actionFilter))
                query = query.Where(a => a.Action == actionFilter);

            var logs = await query.OrderByDescending(a => a.Timestamp).Take(200).ToListAsync();

            ViewBag.SelectedModule = module;
            ViewBag.SelectedAction = actionFilter;

            return View(logs);
        }
    }
}