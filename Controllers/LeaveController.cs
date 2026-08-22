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
        private readonly IAuditService _auditService;

        public LeaveController(
            ILeaveRepository leaveRepository,
            IAttendanceRequestRepository attendanceRequestRepository,
            IAttendanceRepository attendanceRepository,
            IAuditService auditService)
        {
            _leaveRepository = leaveRepository;
            _attendanceRequestRepository = attendanceRequestRepository;
            _attendanceRepository = attendanceRepository;
            _auditService = auditService;
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
                ViewBag.Leaves =
                    await _leaveRepository.GetAllAsync(statusFilter);

                ViewBag.AttendanceRequests =
                    await _attendanceRequestRepository.GetAllAsync(statusFilter);
            }
            else
            {
                int employeeId = GetCurrentEmployeeId();

                ViewBag.Leaves =
                    await _leaveRepository.GetByEmployeeIdAsync(employeeId);

                ViewBag.AttendanceRequests =
                    await _attendanceRequestRepository.GetByEmployeeIdAsync(employeeId);
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

            ModelState.AddModelError(
                nameof(model.RequestType),
                "Please select a valid request type");

            return View(model);
        }

        // =====================================================
        // APPLY LEAVE
        // =====================================================

        private async Task<IActionResult> HandleLeaveApply(
            RequestViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LeaveType))
                ModelState.AddModelError(
                    nameof(model.LeaveType),
                    "Leave type is required");

            if (model.StartDate == null)
                ModelState.AddModelError(
                    nameof(model.StartDate),
                    "Start date is required");

            if (model.EndDate == null)
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "End date is required");

            if (model.StartDate != null &&
                model.EndDate != null &&
                model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "End date cannot be before start date");
            }

            if (!ModelState.IsValid)
            {
                return View("Apply", model);
            }

            int employeeId = GetCurrentEmployeeId();

            var leave = new Leave
            {
                EmployeeId = employeeId,
                LeaveType = model.LeaveType!,
                StartDate = model.StartDate!.Value,
                EndDate = model.EndDate!.Value,
                Reason = model.Reason,
                Status = "Pending",
                AppliedDate = DateTime.Now
            };

            await _leaveRepository.AddAsync(leave);

            // Audit Log
            await _auditService.LogAsync(
                "Leave",
                "Create",
                $"Employee ID {employeeId} applied for {leave.LeaveType} leave from {leave.StartDate:dd-MM-yyyy} to {leave.EndDate:dd-MM-yyyy}."
            );

            NotifySuccess("Leave request submitted successfully.");

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // ATTENDANCE CORRECTION
        // =====================================================

        private async Task<IActionResult> HandleAttendanceCorrectionApply(
            RequestViewModel model)
        {
            if (model.AttendanceDate == null)
            {
                ModelState.AddModelError(
                    nameof(model.AttendanceDate),
                    "Date is required");
            }

            if (string.IsNullOrWhiteSpace(model.RequestedCheckIn) &&
                string.IsNullOrWhiteSpace(model.RequestedCheckOut))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please provide at least a check-in or check-out time");
            }

            if (!ModelState.IsValid)
            {
                return View("Apply", model);
            }

            int employeeId = GetCurrentEmployeeId();

            var request = new AttendanceRequest
            {
                EmployeeId = employeeId,
                Date = model.AttendanceDate!.Value,
                RequestedCheckIn =
                    TimeSpan.TryParse(
                        model.RequestedCheckIn,
                        out var ci)
                        ? ci
                        : null,

                RequestedCheckOut =
                    TimeSpan.TryParse(
                        model.RequestedCheckOut,
                        out var co)
                        ? co
                        : null,

                Reason = model.Reason,
                Status = "Pending",
                AppliedDate = DateTime.Now
            };

            await _attendanceRequestRepository.AddAsync(request);

            // Audit Log
            await _auditService.LogAsync(
                "Attendance",
                "Create",
                $"Employee ID {employeeId} submitted an attendance correction request for {request.Date:dd-MM-yyyy}."
            );

            NotifySuccess(
                "Attendance correction request submitted successfully.");

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // APPROVE LEAVE
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var leave =
                await _leaveRepository.GetByIdAsync(id);

            if (leave != null)
            {
                leave.Status = "Approved";

                await _leaveRepository.UpdateAsync(leave);

                // Audit Log
                await _auditService.LogAsync(
                    "Leave",
                    "Approve",
                    $"Leave request ID {id} approved for Employee ID {leave.EmployeeId}."
                );

                NotifySuccess("Request approved successfully.");
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // REJECT LEAVE
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var leave =
                await _leaveRepository.GetByIdAsync(id);

            if (leave != null)
            {
                leave.Status = "Rejected";

                await _leaveRepository.UpdateAsync(leave);

                // Audit Log
                await _auditService.LogAsync(
                    "Leave",
                    "Reject",
                    $"Leave request ID {id} rejected for Employee ID {leave.EmployeeId}."
                );

                NotifySuccess("Request rejected.");
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // APPROVE ATTENDANCE REQUEST
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAttendanceRequest(int id)
        {
            var request =
                await _attendanceRequestRepository.GetByIdAsync(id);

            if (request == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var allForEmployee =
                await _attendanceRepository.GetAllAsync(
                    request.EmployeeId,
                    null,
                    null);

            var existingAttendance =
                allForEmployee.FirstOrDefault(
                    a => a.Date.Date == request.Date.Date);

            if (existingAttendance != null)
            {
                if (request.RequestedCheckIn.HasValue)
                    existingAttendance.CheckInTime =
                        request.RequestedCheckIn;

                if (request.RequestedCheckOut.HasValue)
                    existingAttendance.CheckOutTime =
                        request.RequestedCheckOut;

                // Safety check
                if (existingAttendance.CheckInTime.HasValue &&
                    existingAttendance.CheckOutTime.HasValue)
                {
                    if (existingAttendance.CheckOutTime.Value <=
                        existingAttendance.CheckInTime.Value)
                    {
                        TempData["ErrorMessage"] =
                            "Cannot approve: the requested check-out time is not after the check-in time. Please review this request manually.";

                        return RedirectToAction(nameof(Index));
                    }

                    existingAttendance.TotalWorkingHours =
                        existingAttendance.CheckOutTime.Value -
                        existingAttendance.CheckInTime.Value;
                }

                existingAttendance.Status = "Present";

                await _attendanceRepository.UpdateAsync(
                    existingAttendance);
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

                if (newAttendance.CheckInTime.HasValue &&
                    newAttendance.CheckOutTime.HasValue)
                {
                    if (newAttendance.CheckOutTime.Value <=
                        newAttendance.CheckInTime.Value)
                    {
                        TempData["ErrorMessage"] =
                            "Cannot approve: the requested check-out time is not after the check-in time. Please review this request manually.";

                        return RedirectToAction(nameof(Index));
                    }

                    newAttendance.TotalWorkingHours =
                        newAttendance.CheckOutTime.Value -
                        newAttendance.CheckInTime.Value;
                }

                newAttendance.Status = "Present";

                await _attendanceRepository.AddAsync(
                    newAttendance);
            }

            request.Status = "Approved";

            await _attendanceRequestRepository.UpdateAsync(
                request);

            // Audit Log
            await _auditService.LogAsync(
                "Attendance",
                "Approve",
                $"Attendance correction request ID {id} approved for Employee ID {request.EmployeeId} for {request.Date:dd-MM-yyyy}."
            );

            NotifySuccess("Request approved successfully.");

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // REJECT ATTENDANCE REQUEST
        // =====================================================

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAttendanceRequest(int id)
        {
            var request =
                await _attendanceRequestRepository.GetByIdAsync(id);

            if (request != null)
            {
                request.Status = "Rejected";

                await _attendanceRequestRepository.UpdateAsync(
                    request);

                // Audit Log
                await _auditService.LogAsync(
                    "Attendance",
                    "Reject",
                    $"Attendance correction request ID {id} rejected for Employee ID {request.EmployeeId} for {request.Date:dd-MM-yyyy}."
                );

                NotifySuccess("Request rejected.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}