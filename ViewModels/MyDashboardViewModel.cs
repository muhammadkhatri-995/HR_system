namespace HR_system.ViewModels
{
    // A DIFFERENT, much narrower ViewModel than DashboardViewModel —
    // deliberately contains ONLY this one employee's own data.
    // Same security principle as ProfileViewModel from Module 11:
    // the shape of the ViewModel itself is what prevents one employee
    // from ever seeing another's information, not just a UI toggle.
    public class MyDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public int DaysPresentThisMonth { get; set; }
        public int DaysAbsentThisMonth { get; set; }

        public int MyPendingLeaves { get; set; }
        public int MyApprovedLeaves { get; set; }
        public int MyRejectedLeaves { get; set; }

        public decimal? LastNetSalary { get; set; }
        public DateTime? LastPayDate { get; set; }

        public bool CheckedInToday { get; set; }
        public bool CheckedOutToday { get; set; }
        public List<AttendancePerformancePoint> MonthlyPerformance { get; set; } = new();
    }
    public class AttendancePerformancePoint
    {
        public string Label { get; set; } = string.Empty;   // e.g. "Aug 1"
        public double HoursWorked { get; set; }              // decimal hours, e.g. 8.5
        public string Status { get; set; } = string.Empty;   // "Complete", "Incomplete", "InProgress"
    }
}