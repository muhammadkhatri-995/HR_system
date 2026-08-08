using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.EntityFrameworkCore;
using static HR_system.ViewModels.DashboardViewModel;

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
            var dashboard = new DashboardViewModel();

            dashboard.TotalEmployees = await _context.Employees.CountAsync();
            dashboard.ActiveEmployees = await _context.Employees.CountAsync(e => e.Status == "Active");
            dashboard.InactiveEmployees = await _context.Employees.CountAsync(e => e.Status == "Inactive");
            dashboard.TotalDepartments = await _context.Departments.CountAsync();
            dashboard.TotalRoles = await _context.Roles.CountAsync();

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            dashboard.NewEmployeesThisMonth = await _context.Employees
                .CountAsync(e => e.CreatedDate >= thirtyDaysAgo);

            // Group by DepartmentId first (safe even if Department is null),
            // then look up the name — this avoids a NullReferenceException
            // for any employee with a missing/invalid DepartmentId.
            dashboard.EmployeesByDepartment = await _context.Employees
                .Include(e => e.Department)
                .GroupBy(e => e.Department != null ? e.Department.Name : "Unassigned")
                .Select(g => new DepartmentCount
                {
                    DepartmentName = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            dashboard.RecentEmployees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .OrderByDescending(e => e.CreatedDate)
                .Take(6)
                .ToListAsync();

            return dashboard;
        }
    }
}