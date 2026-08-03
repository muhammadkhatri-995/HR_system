using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HR_system.Models
{
    public class Employee
    {
        [Key]
        public int id { get; set; } 
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")] // built-in email format validation
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;
        [Required(ErrorMessage = "CNIC is required")]
        [StringLength(20)]
        public string CNIC { get; set; } = string.Empty;

        [StringLength(200)]
        public String? Address { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)] // tells the input to render as a date picker, not date+time
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Joining date is required")]
        [DataType(DataType.Date)]
        public DateTime JoiningDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Please select a department")]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [Required(ErrorMessage = "Please select a role")]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        [Required(ErrorMessage = "Salary is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        // The photo shown on the profile — stores only the file PATH, not raw bytes
        [StringLength(255)]
        public string? EmployeePhoto { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }











}
