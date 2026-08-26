using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace HR_system.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAuditService _auditService;

        public AttendanceController(
            IAttendanceRepository attendanceRepository,
            IEmployeeRepository employeeRepository,
            IAuditService auditService)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
            _auditService = auditService;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // Server chahe kahin bhi (UTC timezone waale cloud host par) chal raha ho,
        // yeh method hamesha Pakistan ka asal wall-clock time deta hai —
        // taake CheckIn/CheckOut hamesha employee ke asal local time se match ho.
        private static TimeZoneInfo GetPakistanTimeZone()
        {
            try
            {
                // Linux servers (Render/Railway) ye IANA ID samajhte hain
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
            }
            catch (TimeZoneNotFoundException)
            {
                // Windows local machine ke liye fallback
                return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
            }
        }

        private DateTime GetPakistanNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetPakistanTimeZone());
        }

        // GET: /Attendance
        public async Task<IActionResult> Index(int? employeeId, int? month, int? year)
        {
            int currentEmployeeId = GetCurrentEmployeeId();

            ViewBag.TodayRecord =
                await _attendanceRepository.GetTodayAttendanceForEmployeeAsync(currentEmployeeId);

            bool isManager = User.IsInRole("Admin") || User.IsInRole("HR");
            ViewBag.IsManager = isManager;

            if (isManager)
            {
                var records = await _attendanceRepository
                    .GetAllAsync(employeeId, month, year);

                var (employees, _) =
                    await _employeeRepository.GetAllAsync(null, 1, 1000);

                ViewBag.Employees = employees
                    .Select(e => new SelectListItem
                    {
                        Value = e.id.ToString(),
                        Text = $"{e.FirstName} {e.LastName}"
                    })
                    .ToList();

                ViewBag.SelectedEmployeeId = employeeId;
                ViewBag.SelectedMonth = month ?? DateTime.Now.Month;
                ViewBag.SelectedYear = year ?? DateTime.Now.Year;

                return View(records);
            }

            return View(new List<Attendence>());
        }

        // POST: /Attendance/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            int employeeId = GetCurrentEmployeeId();

            var existing =
                await _attendanceRepository
                    .GetTodayAttendanceForEmployeeAsync(employeeId);

            if (existing == null)
            {
                var pktNow = GetPakistanNow();
                var attendance = new Attendence
                {
                    EmployeeId = employeeId,
                    Date = pktNow.Date,           
                    CheckInTime = pktNow.TimeOfDay,
                    Status = "Present"
                };

                await _attendanceRepository.AddAsync(attendance);

                await _auditService.LogAsync(
                    "Attendance",
                    "Check In",
                    $"Employee ID {employeeId} checked in at {DateTime.Now:hh:mm tt}."
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Attendance/CheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            int employeeId = GetCurrentEmployeeId();

            var attendance =
                await _attendanceRepository
                    .GetTodayAttendanceForEmployeeAsync(employeeId);

            if (attendance != null &&
                attendance.CheckInTime != null &&
                attendance.CheckOutTime == null)
            {

                var pktNow = GetPakistanNow();
                attendance.CheckOutTime = DateTime.Now.TimeOfDay;

                attendance.TotalWorkingHours =
                    attendance.CheckOutTime.Value -
                    attendance.CheckInTime.Value;

                await _attendanceRepository.UpdateAsync(attendance);

                await _auditService.LogAsync(
                    "Attendance",
                    "Check Out",
                    $"Employee ID {employeeId} checked out at {DateTime.Now:hh:mm tt}."
                );
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CREATE
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            var model = new AttendanceViewModel();

            await PopulateEmployeeDropdown(model);

            return View(model);
        }

        private async Task PopulateEmployeeDropdown(AttendanceViewModel model)
        {
            var (employees, _) =
                await _employeeRepository.GetAllAsync(null, 1, 1000);

            model.Employees = employees
                .Select(e => new SelectListItem
                {
                    Value = e.id.ToString(),
                    Text = $"{e.FirstName} {e.LastName}"
                })
                .ToList();
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttendanceViewModel model)
        {
            bool alreadyMarked =
                await _attendanceRepository
                    .ExistsForEmployeeOnDateAsync(
                        model.EmployeeId,
                        model.Date);

            if (alreadyMarked)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Attendance for this employee on this date is already marked.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown(model);
                return View(model);
            }

            var attendance = new Attendence
            {
                EmployeeId = model.EmployeeId,
                Date = model.Date,
                Status = model.Status,
                CheckInTime =
                    TimeSpan.TryParse(model.CheckIn, out var checkIn)
                        ? checkIn
                        : null,

                CheckOutTime =
                    TimeSpan.TryParse(model.CheckOut, out var checkOut)
                        ? checkOut
                        : null
            };

            if (attendance.CheckInTime.HasValue &&
                attendance.CheckOutTime.HasValue)
            {
                attendance.TotalWorkingHours =
                    attendance.CheckOutTime.Value -
                    attendance.CheckInTime.Value;
            }

            await _attendanceRepository.AddAsync(attendance);

            await _auditService.LogAsync(
                "Attendance",
                "Create",
                $"Attendance created for Employee ID {model.EmployeeId} on {model.Date:dd-MM-yyyy}."
            );

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id)
        {
            var attendance =
                await _attendanceRepository.GetByIdAsync(id);

            if (attendance == null)
                return NotFound();

            var model = new AttendanceViewModel
            {
                Id = attendance.Id,
                EmployeeId = attendance.EmployeeId,
                Date = attendance.Date,
                Status = attendance.Status,
                CheckIn = attendance.CheckInTime?.ToString(@"hh\:mm"),
                CheckOut = attendance.CheckOutTime?.ToString(@"hh\:mm")
            };

            await PopulateEmployeeDropdown(model);

            return View(model);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            AttendanceViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown(model);
                return View(model);
            }

            var attendance =
                await _attendanceRepository.GetByIdAsync(id);

            if (attendance == null)
                return NotFound();

            attendance.EmployeeId = model.EmployeeId;
            attendance.Date = model.Date;
            attendance.Status = model.Status;

            attendance.CheckInTime =
                TimeSpan.TryParse(model.CheckIn, out var checkIn)
                    ? checkIn
                    : null;

            attendance.CheckOutTime =
                TimeSpan.TryParse(model.CheckOut, out var checkOut)
                    ? checkOut
                    : null;

            attendance.TotalWorkingHours =
                attendance.CheckInTime.HasValue &&
                attendance.CheckOutTime.HasValue
                    ? attendance.CheckOutTime.Value -
                      attendance.CheckInTime.Value
                    : null;

            await _attendanceRepository.UpdateAsync(attendance);

            await _auditService.LogAsync(
                "Attendance",
                "Update",
                $"Attendance ID {id} updated for Employee ID {model.EmployeeId}."
            );

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DELETE
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var attendance =
                await _attendanceRepository.GetByIdAsync(id);

            if (attendance == null)
                return NotFound();

            return View(attendance);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance =
                await _attendanceRepository.GetByIdAsync(id);

            if (attendance == null)
                return NotFound();

            int employeeId = attendance.EmployeeId;

            await _attendanceRepository.DeleteAsync(id);

            await _auditService.LogAsync(
                "Attendance",
                "Delete",
                $"Attendance ID {id} deleted for Employee ID {employeeId}."
            );

            return RedirectToAction(nameof(Index));
        }
    }
}