namespace HR_system.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalRoles { get; set; }
        public int NewEmployeesThisMonth { get; set; }

        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;
        public string? CurrentUserPhoto { get; set; }

        public int MaleEmployeesCount { get; set; }
        public int FemaleEmployeesCount { get; set; }

        public List<DepartmentCount> EmployeesByDepartment { get; set; } = new();
        public List<Models.Employee> RecentEmployees { get; set; } = new();

        // ----- NEW for Module 15 -----

        // One entry per day of the current month: how many employees were
        // marked "Present" that day. Powers a line/bar chart showing trends.
        public List<AttendanceTrendPoint> MonthlyAttendanceTrend { get; set; } = new();

        // Counts per leave status, for a simple pie/doughnut chart.
        public int PendingLeaves { get; set; }
        public int ApprovedLeaves { get; set; }
        public int RejectedLeaves { get; set; }

        // Total net salary paid out per month, for the last 6 months.
        public List<PayrollSummaryPoint> PayrollLast6Months { get; set; } = new();
    }

    public class DepartmentCount
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AttendanceTrendPoint
    {
        public string Label { get; set; } = string.Empty; // e.g. "Aug 1"
        public int PresentCount { get; set; }
    }

    public class PayrollSummaryPoint
    {
        public string MonthLabel { get; set; } = string.Empty; // e.g. "Mar 2026"
        public decimal TotalNetSalary { get; set; }
    }



}