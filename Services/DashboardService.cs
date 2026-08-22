using HR_system.Data;
using HR_system.Interfaces;
using HR_system.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HR_system.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        // FIX 1: added the missing "int currentEmployeeId" parameter —
        // the method body already needed it, it just wasn't declared.
        public async Task<DashboardViewModel> GetDashboardDataAsync(int currentEmployeeId)
        {
            var dashboard = new DashboardViewModel();

            // FIX 2: e.Id (capital I) — matches the Employee model's real
            // property name. "e.id" doesn't exist and won't compile.
            var currentUser = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.id == currentEmployeeId);

            if (currentUser != null)
            {
                dashboard.CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}";
                dashboard.CurrentUserRole = currentUser.Role?.Name ?? "Admin";
                dashboard.CurrentUserPhoto = currentUser.EmployeePhoto;
            }

            // ----- Existing stats (unchanged from Module 6) -----
            dashboard.TotalEmployees = await _context.Employees.CountAsync();
            dashboard.ActiveEmployees = await _context.Employees.CountAsync(e => e.Status == "Active");
            dashboard.InactiveEmployees = await _context.Employees.CountAsync(e => e.Status == "Inactive");
            dashboard.TotalDepartments = await _context.Departments.CountAsync();
            dashboard.TotalRoles = await _context.Roles.CountAsync();

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            dashboard.NewEmployeesThisMonth = await _context.Employees.CountAsync(e => e.CreatedDate >= thirtyDaysAgo);

            dashboard.EmployeesByDepartment = await _context.Employees
                .Include(e => e.Department)
                .GroupBy(e => e.Department != null ? e.Department.Name : "Unassigned")
                .Select(g => new DepartmentCount { DepartmentName = g.Key, Count = g.Count() })
                .ToListAsync();

            dashboard.RecentEmployees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .OrderByDescending(e => e.CreatedDate)
                .Take(6)
                .ToListAsync();

            // ----- Monthly Attendance Trend -----
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var attendanceThisMonth = await _context.Attendances
                .Where(a => a.Date >= startOfMonth)
                .ToListAsync();

            dashboard.MonthlyAttendanceTrend = attendanceThisMonth
                .GroupBy(a => a.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new AttendanceTrendPoint
                {
                    Label = g.Key.ToString("MMM d"),
                    PresentCount = g.Count(a => a.Status == "Present")
                })
                .ToList();

            // ----- Leave Statistics -----
            dashboard.PendingLeaves = await _context.Leaves.CountAsync(l => l.Status == "Pending");
            dashboard.ApprovedLeaves = await _context.Leaves.CountAsync(l => l.Status == "Approved");
            dashboard.RejectedLeaves = await _context.Leaves.CountAsync(l => l.Status == "Rejected");

            // ----- Payroll Summary (last 6 months) -----
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var payrollRecords = await _context.PayRolls
                .Where(p => p.PayDate >= sixMonthsAgo)
                .ToListAsync();

            dashboard.PayrollLast6Months = payrollRecords
                .GroupBy(p => new { p.PayDate.Year, p.PayDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new PayrollSummaryPoint
                {
                    MonthLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalNetSalary = g.Sum(p => p.NetSalary)
                })
                .ToList();

            return dashboard;
        }

        public async Task<MyDashboardViewModel> GetMyDashboardDataAsync(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.id == employeeId); // FIX: e.Id, not e.id

            var model = new MyDashboardViewModel();

            if (employee == null)
            {
                return model;
            }

            model.FullName = $"{employee.FirstName} {employee.LastName}";
            model.PhotoPath = employee.EmployeePhoto;
            model.DepartmentName = employee.Department?.Name ?? "Unassigned";
            model.RoleName = employee.Role?.Name ?? "Employee";
            model.Status = employee.Status;

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var myAttendanceThisMonth = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Date >= startOfMonth)
                .ToListAsync();

            model.DaysPresentThisMonth = myAttendanceThisMonth.Count(a => a.Status == "Present");
            model.DaysAbsentThisMonth = myAttendanceThisMonth.Count(a => a.Status == "Absent");

            model.MyPendingLeaves = await _context.Leaves.CountAsync(l => l.EmployeeId == employeeId && l.Status == "Pending");
            model.MyApprovedLeaves = await _context.Leaves.CountAsync(l => l.EmployeeId == employeeId && l.Status == "Approved");
            model.MyRejectedLeaves = await _context.Leaves.CountAsync(l => l.EmployeeId == employeeId && l.Status == "Rejected");

            var lastPayroll = await _context.PayRolls
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.PayDate)
                .FirstOrDefaultAsync();

            if (lastPayroll != null)
            {
                model.LastNetSalary = lastPayroll.NetSalary;
                model.LastPayDate = lastPayroll.PayDate;
            }

            var todayAttendance = myAttendanceThisMonth.FirstOrDefault(a => a.Date.Date == DateTime.Today);
            model.CheckedInToday = todayAttendance?.CheckInTime != null;
            model.CheckedOutToday = todayAttendance?.CheckOutTime != null;

            return model;
        }
    }
}