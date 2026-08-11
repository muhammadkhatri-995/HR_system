using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.EntityFrameworkCore;

namespace HR_system.Repositories
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly ApplicationDbContext _context;

        public PayrollRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PayRoll>> GetAllAsync(int? employeeId)
        {
            var query = _context.PayRolls.Include(p => p.Employee).AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(p => p.EmployeeId == employeeId.Value);
            }

            return await query.OrderByDescending(p => p.PayDate).ToListAsync();
        }

        public async Task<PayRoll?> GetByIdAsync(int id)
        {
            return await _context.PayRolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(PayRoll payroll)
        {
            await _context.PayRolls.AddAsync(payroll);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var payroll = await _context.PayRolls.FindAsync(id);
            if (payroll != null)
            {
                _context.PayRolls.Remove(payroll);
                await _context.SaveChangesAsync();
            }
        }
    }
}