using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_system.Models
{
    public class Attendence
    {
      
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Please select an employee")]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }
        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }
       
        public TimeSpan? CheckInTime { get; set; }
       
        public TimeSpan? CheckOutTime { get; set; }

        public TimeSpan? TotalWorkingHours { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Present";
    }
    
}
