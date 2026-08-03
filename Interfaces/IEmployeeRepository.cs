using HR_system.Models;

namespace HR_system.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<(IEnumerable<Employee> Employees, int TotalCount)> GetAllAsync(string? searchTerm, int pageNumber, int pageSize);
        Task<Employee?> GetByIdAsync(int id);
        Task AddAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        Task DeleteAsync(int id);

    }
}
