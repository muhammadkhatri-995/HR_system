using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HR_system.Controllers
{
    [Authorize]
    public class LeaveController : BaseController
    {
        private readonly ILeaveRepository _leaveRepository;
        private readonly IAttendanceRequestRepository _attendanceRequestRepository;
        private readonly IAttendanceRepository _attendanceRepository;

        public LeaveController(
            ILeaveRepository leaveRepository,
            IAttendanceRequestRepository attendanceRequestRepository,
            IAttendanceRepository attendanceRepository)
        {
            _leaveRepository = leaveRepository;
            _attendanceRequestRepository = attendanceRequestRepository;
            _attendanceRepository = attendanceRepository;
        }

        private int GetCurrentEmployeeId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }

        // GET: /Leave
        public async Task<IActionResult> Index(string? statusFilter)
        {
            bool isManager = User.IsInRole("Admin") || User.IsInRole("HR");
            ViewBag.IsManager = isManager;
            ViewBag.StatusFilter = statusFilter;

            if (isManager)
            {
                ViewBag.Leaves = await _leaveRepository.GetAllAsync(statusFilter);
                ViewBag.AttendanceRequests = await _attendanceRequestRepository.GetAllAsync(statusFilter);
            }
            else
            {
                int employeeId = GetCurrentEmployeeId();
                ViewBag.Leaves = await _leaveRepository.GetByEmployeeIdAsync(employeeId);
                ViewBag.AttendanceRequests = await _attendanceRequestRepository.GetByEmployeeIdAsync(employeeId);
            }

            return View();
        }

        // GET: /Leave/Apply
        public IActionResult Apply()
        {
            return View(new RequestViewModel());
        }

        // POST: /Leave/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(RequestViewModel model)
        {
            if (model.RequestType == "Leave")
            {
                return await HandleLeaveApply(model);
            }
            else if (model.RequestType == "AttendanceCorrection")
            {
                return await HandleAttendanceCorrectionApply(model);
            }

            ModelState.AddModelError(nameof(model.RequestType), "Please select a valid request type");
            return View(model);
        }

        private async Task<IActionResult> HandleLeaveApply(RequestViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LeaveType))
                ModelState.AddModelError(nameof(model.LeaveType), "Leave type is required");

            if (model.StartDate == null)
                ModelState.AddModelError(nameof(model.StartDate), "Start date is required");

            if (model.EndDate == null)
                ModelState.AddModelError(nameof(model.EndDate), "End date is required");

            if (model.StartDate != null && model.EndDate != null && model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before start date");

            if (!ModelState.IsValid)
            {
                return View("Apply", model);
            }

            var leave = new Leave
            {
                EmployeeId = GetCurrentEmployeeId(),
                LeaveType = model.LeaveType!,
                StartDate = model.StartDate!.Value,
                EndDate = model.EndDate!.Value,
                Reason = model.Reason,
                Status = "Pending",
                AppliedDate = DateTime.Now
            };

            await _leaveRepository.AddAsync(leave);
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> HandleAttendanceCorrectionApply(RequestViewModel model)
        {
            if (model.AttendanceDate == null)
                ModelState.AddModelError(nameof(model.AttendanceDate), "Date is required");

            if (string.IsNullOrWhiteSpace(model.RequestedCheckIn) && string.IsNullOrWhiteSpace(model.RequestedCheckOut))
                ModelState.AddModelError(string.Empty, "Please provide at least a check-in or check-out time");

            if (!ModelState.IsValid)
            {
                return View("Apply", model);
            }

            var request = new AttendanceRequest
            {
                EmployeeId = GetCurrentEmployeeId(),
                Date = model.AttendanceDate!.Value,
                RequestedCheckIn = TimeSpan.TryParse(model.RequestedCheckIn, out var ci) ? ci : null,
                RequestedCheckOut = TimeSpan.TryParse(model.RequestedCheckOut, out var co) ? co : null,
                Reason = model.Reason,
                Status = "Pending",
                AppliedDate = DateTime.Now
            };

            await _attendanceRequestRepository.AddAsync(request);

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _leaveRepository.GetByIdAsync(id);
            if (leave != null)
            {
                leave.Status = "Approved";
                await _leaveRepository.UpdateAsync(leave);
                NotifySuccess("Request approved successfully.");
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var leave = await _leaveRepository.GetByIdAsync(id);
            if (leave != null)
            {
                leave.Status = "Rejected";
                await _leaveRepository.UpdateAsync(leave);
                NotifySuccess("Request rejected.");
            }
            return RedirectToAction(nameof(Index));
        }

        
        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAttendanceRequest(int id)
        {
            var request = await _attendanceRequestRepository.GetByIdAsync(id);
            if (request == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var allForEmployee = await _attendanceRepository.GetAllAsync(request.EmployeeId, null, null);
            var existingAttendance = allForEmployee.FirstOrDefault(a => a.Date.Date == request.Date.Date);

            if (existingAttendance != null)
            {
                if (request.RequestedCheckIn.HasValue)
                    existingAttendance.CheckInTime = request.RequestedCheckIn;

                if (request.RequestedCheckOut.HasValue)
                    existingAttendance.CheckOutTime = request.RequestedCheckOut;

                // SAFETY CHECK: only calculate TotalWorkingHours if CheckOut is
                // genuinely AFTER CheckIn. If not, something's wrong with the
                // submitted times (e.g. employee mixed up AM/PM, or picked a
                // check-out time that's actually earlier than check-in) —
                // reject the approval instead of saving a broken negative duration.
                if (existingAttendance.CheckInTime.HasValue && existingAttendance.CheckOutTime.HasValue)
                {
                    if (existingAttendance.CheckOutTime.Value <= existingAttendance.CheckInTime.Value)
                    {
                        TempData["ErrorMessage"] = "Cannot approve: the requested check-out time is not after the check-in time. Please review this request manually.";
                        return RedirectToAction(nameof(Index));
                    }

                    existingAttendance.TotalWorkingHours = existingAttendance.CheckOutTime.Value - existingAttendance.CheckInTime.Value;
                }

                existingAttendance.Status = "Present";
                await _attendanceRepository.UpdateAsync(existingAttendance);
                NotifySuccess("Request approved successfully.");
            }
            else
            {
                var newAttendance = new Attendence
                {
                    EmployeeId = request.EmployeeId,
                    Date = request.Date,
                    CheckInTime = request.RequestedCheckIn,
                    CheckOutTime = request.RequestedCheckOut
                };

                if (newAttendance.CheckInTime.HasValue && newAttendance.CheckOutTime.HasValue)
                {
                    if (newAttendance.CheckOutTime.Value <= newAttendance.CheckInTime.Value)
                    {
                        TempData["ErrorMessage"] = "Cannot approve: the requested check-out time is not after the check-in time. Please review this request manually.";
                        return RedirectToAction(nameof(Index));
                    }

                    newAttendance.TotalWorkingHours = newAttendance.CheckOutTime.Value - newAttendance.CheckInTime.Value;
                }

                newAttendance.Status = "Present";
                await _attendanceRepository.AddAsync(newAttendance);
            }

            request.Status = "Approved";
            await _attendanceRequestRepository.UpdateAsync(request);

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAttendanceRequest(int id)
        {
            var request = await _attendanceRequestRepository.GetByIdAsync(id);
            if (request != null)
            {
                request.Status = "Rejected";
                await _attendanceRequestRepository.UpdateAsync(request);
                NotifySuccess("Request rejected.");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}