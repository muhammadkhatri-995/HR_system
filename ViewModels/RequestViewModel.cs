using System.ComponentModel.DataAnnotations;
namespace HR_system.ViewModels
{
    public class RequestViewModel
    {
        [Required(ErrorMessage = "Please select a request type")]
        public string RequestType { get; set; } = string.Empty;

        public string? LeaveType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime? AttendanceDate { get; set; }
        public string? RequestedCheckIn { get; set; }
        public string? RequestedCheckOut { get; set; }

        [Required(ErrorMessage = "Please provide a reason")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;





    }
}
