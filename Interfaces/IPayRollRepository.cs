using HR_system.Models;

namespace HR_system.Interfaces
{
    public interface IPayrollRepository
    {
        Task<IEnumerable<PayRoll>> GetAllAsync(int? employeeId);
        Task<PayRoll?> GetByIdAsync(int id);
        Task AddAsync(PayRoll payroll);
        Task DeleteAsync(int id);
    }
}