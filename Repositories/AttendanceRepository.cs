using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.EntityFrameworkCore;

namespace HR_system.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Attendence>> GetAllAsync(int? employeeId, int? month, int? year)
        {
            var query = _context.Attendances.Include(a => a.Employee).AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            if (month.HasValue)
                query = query.Where(a => a.Date.Month == month.Value);

            if (year.HasValue)
                query = query.Where(a => a.Date.Year == year.Value);

            return await query.OrderByDescending(a => a.Date).ToListAsync();
        }

        public async Task<Attendence?> GetByIdAsync(int id)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAsync(Attendence attendance)
        {
            await _context.Attendances.AddAsync(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Attendence attendance)
        {
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsForEmployeeOnDateAsync(int employeeId, DateTime date)
        {
            return await _context.Attendances
                .AnyAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);
        }

        public async Task<Attendence?> GetTodayAttendanceForEmployeeAsync(int employeeId)
        {
            var today = DateTime.Today;
            return await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);
        }
    }
}