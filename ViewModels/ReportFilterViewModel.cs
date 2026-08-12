using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_system.ViewModels
{
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "Employees"; // Employees, Departments, Attendance, Leave, Payroll
        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Employees { get; set; } = new();
    }
}