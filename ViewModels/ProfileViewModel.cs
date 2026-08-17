using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace HR_system.ViewModels
{
    // Deliberately small — only what an employee is allowed to change
    // about THEMSELVES. No Salary, DepartmentId, RoleId, or Status here.
    public class ProfileViewModel
    {
        public int Id { get; set; }

        // Shown read-only on the page (not editable, but useful context)
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? ExistingPhotoPath { get; set; }

        // Actually editable fields
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }

        public IFormFile? PhotoFile { get; set; }
    }
}