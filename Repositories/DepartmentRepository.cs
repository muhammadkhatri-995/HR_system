using HR_system.Data;
using HR_system.Interfaces;
using HR_system.Models;
using Microsoft.EntityFrameworkCore;

namespace HR_system.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        // This class contains the ACTUAL database code (EF Core queries).
        // Controllers never talk to the database directly — they talk to
        // this Repository through the IDepartmentRepository interface.
        private readonly ApplicationDbContext _context;
        // ApplicationDbContext is injected automatically by ASP.NET Core's DI system
        public DepartmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<Department?> GetByIdAsync(int id)
        {
            return await _context.Departments.FindAsync(id);
        }

        public async Task AddAsync(Department department)
        {
            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Department department)
        {
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
        }
    }
}
