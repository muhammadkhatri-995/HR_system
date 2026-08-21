using System.ComponentModel.DataAnnotations;
namespace HR_system.Models
{
    public class AuditLogs
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty; // Employee, Department, Payroll, etc.

        [StringLength(500)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}
