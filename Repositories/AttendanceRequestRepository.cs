using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.EntityFrameworkCore;

namespace HR_system.Repositories
{
    public class AttendanceRequestRepository : IAttendanceRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AttendanceRequest>> GetAllAsync(string? statusFilter)
        {
            var query = _context.AttendanceRequests.Include(r => r.Employee).AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            return await query.OrderByDescending(r => r.AppliedDate).ToListAsync();
        }

        public async Task<IEnumerable<AttendanceRequest>> GetByEmployeeIdAsync(int employeeId)
        {
            return await _context.AttendanceRequests
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.AppliedDate)
                .ToListAsync();
        }

        public async Task<AttendanceRequest?> GetByIdAsync(int id)
        {
            return await _context.AttendanceRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task AddAsync(AttendanceRequest request)
        {
            await _context.AttendanceRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AttendanceRequest request)
        {
            _context.AttendanceRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var request = await _context.AttendanceRequests.FindAsync(id);
            if (request != null)
            {
                _context.AttendanceRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
        }
    }
}