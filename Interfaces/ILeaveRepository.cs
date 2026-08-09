using HR_system.Models;

namespace HR_system.Interfaces
{
    public interface ILeaveRepository
    {
        Task<IEnumerable<Leave>> GetAllAsync(string? statusFilter);
        Task<IEnumerable<Leave>> GetByEmployeeIdAsync(int employeeId);
        Task<Leave?> GetByIdAsync(int id);
        Task AddAsync(Leave leave);
        Task UpdateAsync(Leave leave);
        Task DeleteAsync(int id);
    }
}