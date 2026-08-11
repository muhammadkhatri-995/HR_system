using HR_system.Models;
using Microsoft.EntityFrameworkCore;
namespace HR_system.Data
{
    public class ApplicationDbContext : DbContext
    {
        // This constructor lets ASP.NET Core inject configuration
        // (like the connection string) automatically via Dependency Injection.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Role> Roles { get; set; }

        public DbSet<Employee> Employees { get; set; }

        // public DbSet<Attendence> Attendences { get; set; }
        public DbSet<Attendence> Attendances { get; set; }

        public DbSet<Leave> Leaves { get; set; }
        public DbSet<AttendanceRequest> AttendanceRequests { get; set; } 
        
        public DbSet<PayRoll> PayRolls { get; set; }
    }
}