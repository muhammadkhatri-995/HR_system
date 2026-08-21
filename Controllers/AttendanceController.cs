using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace HR_system.Controllers
{
    // [Authorize] with no Roles = any logged-in employee can reach this
    // controller (needed for punch in/out). Individual actions below add
    // [Authorize(Roles = "Admin,HR")] where HR-only management is required.
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
       // private readonly IAuditService _auditService;
        public AttendanceController(
            IAttendanceRepository attendanceRepository,
            IEmployeeRepository employeeRepository)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // GET: /Attendance
        // ONE page: punch clock for the logged-in user, PLUS (if Admin/HR)
        // the full attendance management table below it.
        public async Task<IActionResult> Index(int? employeeId, int? month, int? year)
        {
            int currentEmployeeId = GetCurrentEmployeeId();
            ViewBag.TodayRecord = await _attendanceRepository.GetTodayAttendanceForEmployeeAsync(currentEmployeeId);

            bool isManager = User.IsInRole("Admin") || User.IsInRole("HR");
            ViewBag.IsManager = isManager;

            if (isManager)
            {
                var records = await _attendanceRepository.GetAllAsync(employeeId, month, year);

                var (employees, _) = await _employeeRepository.GetAllAsync(null, 1, 1000);
                ViewBag.Employees = employees
                    .Select(e => new SelectListItem { Value = e.id.ToString(), Text = $"{e.FirstName} {e.LastName}" })
                    .ToList();

                ViewBag.SelectedEmployeeId = employeeId;
                ViewBag.SelectedMonth = month ?? DateTime.Now.Month;
                ViewBag.SelectedYear = year ?? DateTime.Now.Year;

                return View(records);
            }

            // Non-managers only see their own punch clock — no need to load the full table.
            return View(new List<Attendence>());
        }

        // POST: /Attendance/CheckIn — self check-in, any logged-in employee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            int employeeId = GetCurrentEmployeeId();

            var existing = await _attendanceRepository.GetTodayAttendanceForEmployeeAsync(employeeId);
            if (existing == null)
            {
                var attendance = new Attendence
                {
                    EmployeeId = employeeId,
                    Date = DateTime.Today,
                    CheckInTime = DateTime.Now.TimeOfDay,
                    Status = "Present"
                };
                await _attendanceRepository.AddAsync(attendance);
                
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Attendance/CheckOut — self check-out, any logged-in employee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            int employeeId = GetCurrentEmployeeId();

            var attendance = await _attendanceRepository.GetTodayAttendanceForEmployeeAsync(employeeId);

            if (attendance != null && attendance.CheckInTime != null && attendance.CheckOutTime == null)
            {
                attendance.CheckOutTime = DateTime.Now.TimeOfDay;
                attendance.TotalWorkingHours = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;
                await _attendanceRepository.UpdateAsync(attendance);
            }

            return RedirectToAction(nameof(Index));
        }

        // ----- Everything below is HR/Admin-only management (unchanged logic) -----

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            var model = new AttendanceViewModel();
            await PopulateEmployeeDropdown(model);
            return View(model);
        }

        private async Task PopulateEmployeeDropdown(AttendanceViewModel model)
        {
            var (employees, _) = await _employeeRepository.GetAllAsync(null, 1, 1000);
            model.Employees = employees
                .Select(e => new SelectListItem { Value = e.id.ToString(), Text = $"{e.FirstName} {e.LastName}" })
                .ToList();
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttendanceViewModel model)
        {
            bool alreadyMarked = await _attendanceRepository.ExistsForEmployeeOnDateAsync(model.EmployeeId, model.Date);
            if (alreadyMarked)
            {
                ModelState.AddModelError(string.Empty, "Attendance for this employee on this date is already marked.");
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
                CheckInTime = TimeSpan.TryParse(model.CheckIn, out var checkIn) ? checkIn : null,
                CheckOutTime = TimeSpan.TryParse(model.CheckOut, out var checkOut) ? checkOut : null
            };

            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
            {
                attendance.TotalWorkingHours = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;
            }

            await _attendanceRepository.AddAsync(attendance);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id)
        {
            var attendance = await _attendanceRepository.GetByIdAsync(id);
            if (attendance == null) return NotFound();

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
        public async Task<IActionResult> Edit(int id, AttendanceViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateEmployeeDropdown(model);
                return View(model);
            }

            var attendance = await _attendanceRepository.GetByIdAsync(id);
            if (attendance == null) return NotFound();

            attendance.EmployeeId = model.EmployeeId;
            attendance.Date = model.Date;
            attendance.Status = model.Status;
            attendance.CheckInTime = TimeSpan.TryParse(model.CheckIn, out var checkIn) ? checkIn : null;
            attendance.CheckOutTime = TimeSpan.TryParse(model.CheckOut, out var checkOut) ? checkOut : null;

            attendance.TotalWorkingHours = (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                ? attendance.CheckOutTime.Value - attendance.CheckInTime.Value
                : null;

            await _attendanceRepository.UpdateAsync(attendance);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var attendance = await _attendanceRepository.GetByIdAsync(id);
            if (attendance == null) return NotFound();
            return View(attendance);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _attendanceRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}