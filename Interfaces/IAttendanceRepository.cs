using HR_system.Models;

namespace HR_system.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<IEnumerable<Attendence>> GetAllAsync(int? employeeId, int? month, int? year);
        Task<Attendence?> GetByIdAsync(int id);
        Task AddAsync(Attendence attendence);
        Task UpdateAsync(Attendence attendence);
        Task DeleteAsync(int id);
        Task<bool> ExistsForEmployeeOnDateAsync(int employeeId, DateTime date);
        Task<Attendence?> GetTodayAttendanceForEmployeeAsync(int employeeId);
    }
}