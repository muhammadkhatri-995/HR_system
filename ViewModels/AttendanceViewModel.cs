using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HR_system.ViewModels
{
    public class AttendanceViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select an employee")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }

        [Required]
        public string Status { get; set; } = "Present";

        public List<SelectListItem> Employees { get; set; } = new();
    }
}