using HR_system.ViewModels;

namespace HR_system.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(int currentEmployeeId);   // ← now matches
        Task<MyDashboardViewModel> GetMyDashboardDataAsync(int employeeId);
    }
}