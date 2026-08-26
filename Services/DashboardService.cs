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

        // Helper Method: Linux Render Containers par exact local PKT Timezone fetch karne ke liye
        private static DateTime GetPakistanNow()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(int currentEmployeeId)
        {
            var dashboard = new DashboardViewModel();
            var pktNow = GetPakistanNow();

            var currentUser = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.id == currentEmployeeId);

            if (currentUser != null)
            {
                dashboard.CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}";
                dashboard.CurrentUserRole = currentUser.Role?.Name ?? "Admin";
                dashboard.CurrentUserPhoto = currentUser.EmployeePhoto;
            }

            // ----- Stats -----
            dashboard.TotalEmployees = await _context.Employees.CountAsync();
            dashboard.ActiveEmployees = await _context.Employees.CountAsync(e => e.Status == "Active");
            dashboard.InactiveEmployees = await _context.Employees.CountAsync(e => e.Status == "Inactive");
            dashboard.TotalDepartments = await _context.Departments.CountAsync();
            dashboard.TotalRoles = await _context.Roles.CountAsync();

            // ----- Gender Distribution (Pie Chart) -----
            dashboard.MaleEmployeesCount = await _context.Employees.CountAsync(e => e.Gender == "Male");
            dashboard.FemaleEmployeesCount = await _context.Employees.CountAsync(e => e.Gender == "Female");

            var thirtyDaysAgo = pktNow.AddDays(-30);
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
            var startOfMonth = new DateTime(pktNow.Year, pktNow.Month, 1);
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
            var sixMonthsAgo = pktNow.AddMonths(-6);
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
                .FirstOrDefaultAsync(e => e.id == employeeId);

            var model = new MyDashboardViewModel();
            var pktNow = GetPakistanNow();

            if (employee == null)
            {
                return model;
            }

            model.FullName = $"{employee.FirstName} {employee.LastName}";
            model.PhotoPath = employee.EmployeePhoto;
            model.DepartmentName = employee.Department?.Name ?? "Unassigned";
            model.RoleName = employee.Role?.Name ?? "Employee";
            model.Status = employee.Status;

            var startOfMonth = new DateTime(pktNow.Year, pktNow.Month, 1);

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

            var todayAttendance = myAttendanceThisMonth.FirstOrDefault(a => a.Date.Date == pktNow.Date);
            model.CheckedInToday = todayAttendance?.CheckInTime != null;
            model.CheckedOutToday = todayAttendance?.CheckOutTime != null;

            // ----- Monthly Performance Chart Data -----
            model.MonthlyPerformance = myAttendanceThisMonth
                .OrderBy(a => a.Date)
                .Select(a =>
                {
                    var point = new AttendancePerformancePoint
                    {
                        Label = a.Date.ToString("MMM d")
                    };

                    if (a.CheckInTime.HasValue && a.CheckOutTime.HasValue)
                    {
                        var worked = a.CheckOutTime.Value - a.CheckInTime.Value;
                        point.HoursWorked = Math.Round(worked.TotalHours, 2);
                        point.Status = point.HoursWorked >= 9 ? "Complete" : "Incomplete";
                    }
                    else if (a.CheckInTime.HasValue && !a.CheckOutTime.HasValue)
                    {
                        var elapsed = pktNow.TimeOfDay - a.CheckInTime.Value;
                        point.HoursWorked = elapsed.TotalHours > 0 ? Math.Round(elapsed.TotalHours, 2) : 0;
                        point.Status = "InProgress";
                    }
                    else
                    {
                        point.HoursWorked = 0;
                        point.Status = "Incomplete";
                    }

                    return point;
                })
                .ToList();

            return model;
        }
    }
}