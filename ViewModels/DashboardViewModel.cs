namespace HR_system.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalRoles { get; set; }

        // Employees added in the last 30 days
        public int NewEmployeesThisMonth { get; set; }

        // Correct type: a List of our own DepartmentCount class (defined below,
        // as a SEPARATE class, not nested inside this one).
        public List<DepartmentCount> EmployeesByDepartment { get; set; } = new();

        public List<Models.Employee> RecentEmployees { get; set; } = new();
    }

    // This is a SEPARATE, sibling class — same file is fine, but it must NOT
    // be nested inside DashboardViewModel's curly braces.
    public class DepartmentCount
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Count { get; set; } // plain public property, not a raw internal field
    }
}






