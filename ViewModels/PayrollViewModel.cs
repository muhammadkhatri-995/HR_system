using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HR_system.ViewModels
{
    public class PayrollViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select an employee")]
        public int EmployeeId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Bonus cannot be negative")]
        public decimal Bonus { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Deduction cannot be negative")]
        public decimal Deduction { get; set; } = 0;

        [Required]
        [DataType(DataType.Date)]
        public DateTime PayDate { get; set; } = DateTime.Today;

        public List<SelectListItem> Employees { get; set; } = new();
    }
}