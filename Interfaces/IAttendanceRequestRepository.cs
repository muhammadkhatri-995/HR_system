using HR_system.Models;

namespace HR_system.Interfaces
{
    public interface IAttendanceRequestRepository
    {
        Task<IEnumerable<AttendanceRequest>> GetAllAsync(string? statusFilter);
        Task<IEnumerable<AttendanceRequest>> GetByEmployeeIdAsync(int employeeId);
        Task<AttendanceRequest?> GetByIdAsync(int id);
        Task AddAsync(AttendanceRequest request);
        Task UpdateAsync(AttendanceRequest request);
        Task DeleteAsync(int id);
    }
}