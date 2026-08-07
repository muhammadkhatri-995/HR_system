using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HR_system.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Employee> Employees, int TotalCount)> GetAllAsync(string? searchTerm, int pageNumber, int pageSize)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e =>
                    e.FirstName.Contains(searchTerm) ||
                    e.LastName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var employees = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (employees, totalCount);
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.id == id);
        }

        public async Task AddAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.Email == email);
        }

        public async Task UpdateAsync(Employee employee)
        {
            _context.Entry(employee).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var totalEmployees = await _context.Employees.CountAsync();
            var activeEmployees = await _context.Employees.CountAsync(e => e.Status == "Active"
            );
            var inactiveEmployees = totalEmployees - activeEmployees;
            var totalDepartments = await _context.Departments.CountAsync();
            var totalRoles = await _context.Roles.CountAsync();
            var newEmployeesThisMonth = await _context.Employees
                .CountAsync(e => e.CreatedDate >= DateTime.UtcNow.AddDays(-30));
            var recentEmployees = await _context.Employees
                .OrderByDescending(e => e.CreatedDate)
                .Take(5)
                .ToListAsync();
            return new DashboardViewModel
            {
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                InactiveEmployees = inactiveEmployees,
                TotalDepartments = totalDepartments,
                TotalRoles = totalRoles,
                NewEmployeesThisMonth = newEmployeesThisMonth,
                RecentEmployees = recentEmployees
            };
        }
    }
}