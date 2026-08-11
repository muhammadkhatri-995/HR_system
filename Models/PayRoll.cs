using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HR_system.Models
{
    public class PayRoll
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bonus { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Deduction { get; set; } = 0;

        // Calculated once when the payroll is created, then stored permanently.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PayDate { get; set; } = DateTime.Today;

    }
}
