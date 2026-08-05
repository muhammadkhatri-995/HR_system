using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace HR_system.ViewModels
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;
        [Required]
        public string CNIC { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime JoiningDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Please select a department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Please select a role")]
        public int RoleId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Salary { get; set; }
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string Status { get; set; } = "Active";
        public IFormFile? PhotoFile { get; set; }
        public string? ExistingPhotoPath { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? Departments { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? Roles{ get; set; }





    }
}
