using ClosedXML.Excel;
using HR_system.Interfaces;
using HR_system.Models;
using HR_system.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HR_system.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ReportController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILeaveRepository _leaveRepository;
        private readonly IPayrollRepository _payrollRepository;
        private readonly IAuditService _auditService;

        public ReportController(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IAttendanceRepository attendanceRepository,
            ILeaveRepository leaveRepository,
            IPayrollRepository payrollRepository,
            IAuditService auditService)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _attendanceRepository = attendanceRepository;
            _leaveRepository = leaveRepository;
            _payrollRepository = payrollRepository;
            _auditService = auditService;
        }

        // GET: /Report
        public async Task<IActionResult> Index(
            ReportFilterViewModel filter)
        {
            await PopulateDropdowns(filter);

            return View(filter);
        }

        private async Task PopulateDropdowns(
            ReportFilterViewModel filter)
        {
            var departments =
                await _departmentRepository.GetAllAsync();

            filter.Departments = departments
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToList();

            var (employees, _) =
                await _employeeRepository.GetAllAsync(
                    null,
                    1,
                    1000);

            filter.Employees = employees
                .Select(e => new SelectListItem
                {
                    Value = e.id.ToString(),
                    Text = $"{e.FirstName} {e.LastName}"
                })
                .ToList();
        }

        // =====================================================
        // GET REPORT DATA
        // =====================================================

        private async Task<object> GetReportData(
            ReportFilterViewModel filter)
        {
            switch (filter.ReportType)
            {
                case "Employees":

                    var (employees, _) =
                        await _employeeRepository.GetAllAsync(
                            null,
                            1,
                            10000);

                    if (filter.DepartmentId.HasValue)
                    {
                        employees = employees.Where(
                            e => e.DepartmentId ==
                                 filter.DepartmentId.Value);
                    }

                    return employees.ToList();

                case "Departments":

                    return (
                        await _departmentRepository
                            .GetAllAsync()
                    ).ToList();

                case "Attendance":

                    var attendance =
                        await _attendanceRepository
                            .GetAllAsync(
                                filter.EmployeeId,
                                null,
                                null);

                    if (filter.FromDate.HasValue)
                    {
                        attendance = attendance.Where(
                            a => a.Date >=
                                 filter.FromDate.Value);
                    }

                    if (filter.ToDate.HasValue)
                    {
                        attendance = attendance.Where(
                            a => a.Date <=
                                 filter.ToDate.Value);
                    }

                    return attendance.ToList();

                case "Leave":

                    var leaves =
                        await _leaveRepository
                            .GetAllAsync(null);

                    if (filter.EmployeeId.HasValue)
                    {
                        leaves = leaves.Where(
                            l => l.EmployeeId ==
                                 filter.EmployeeId.Value);
                    }

                    return leaves.ToList();

                case "Payroll":

                    return (
                        await _payrollRepository
                            .GetAllAsync(
                                filter.EmployeeId)
                    ).ToList();

                default:

                    return new List<object>();
            }
        }

        // =====================================================
        // REPORT PREVIEW
        // =====================================================

        public async Task<IActionResult> Preview(
            ReportFilterViewModel filter)
        {
            var data =
                await GetReportData(filter);

            ViewBag.ReportType =
                filter.ReportType;

            ViewBag.Filter = filter;

            // Audit Log
            await _auditService.LogAsync(
                "Report",
                "Preview",
                BuildReportLogDetails(
                    filter,
                    "previewed")
            );

            return PartialView(
                "_ReportPreview",
                data);
        }

        // =====================================================
        // EXPORT EXCEL
        // =====================================================

        public async Task<IActionResult> ExportExcel(
            ReportFilterViewModel filter)
        {
            var data =
                await GetReportData(filter);

            using var workbook =
                new XLWorkbook();

            var sheet =
                workbook.Worksheets.Add(
                    filter.ReportType);

            switch (filter.ReportType)
            {
                case "Employees":

                    var employees =
                        (List<Employee>)data;

                    sheet.Cell(1, 1).Value =
                        "First Name";

                    sheet.Cell(1, 2).Value =
                        "Last Name";

                    sheet.Cell(1, 3).Value =
                        "Email";

                    sheet.Cell(1, 4).Value =
                        "Department";

                    sheet.Cell(1, 5).Value =
                        "Role";

                    sheet.Cell(1, 6).Value =
                        "Status";

                    int row = 2;

                    foreach (var e in employees)
                    {
                        sheet.Cell(row, 1).Value =
                            e.FirstName;

                        sheet.Cell(row, 2).Value =
                            e.LastName;

                        sheet.Cell(row, 3).Value =
                            e.Email;

                        sheet.Cell(row, 4).Value =
                            e.Department?.Name ?? "";

                        sheet.Cell(row, 5).Value =
                            e.Role?.Name ?? "";

                        sheet.Cell(row, 6).Value =
                            e.Status;

                        row++;
                    }

                    break;

                case "Attendance":

                    var records =
                        (List<Attendence>)data;

                    sheet.Cell(1, 1).Value =
                        "Employee";

                    sheet.Cell(1, 2).Value =
                        "Date";

                    sheet.Cell(1, 3).Value =
                        "Check In";

                    sheet.Cell(1, 4).Value =
                        "Check Out";

                    sheet.Cell(1, 5).Value =
                        "Total Hours";

                    int r = 2;

                    foreach (var a in records)
                    {
                        sheet.Cell(r, 1).Value =
                            a.Employee != null
                                ? $"{a.Employee.FirstName} {a.Employee.LastName}"
                                : "";

                        sheet.Cell(r, 2).Value =
                            a.Date.ToShortDateString();

                        sheet.Cell(r, 3).Value =
                            a.CheckInTime?
                                .ToString(@"hh\:mm") ?? "-";

                        sheet.Cell(r, 4).Value =
                            a.CheckOutTime?
                                .ToString(@"hh\:mm") ?? "-";

                        sheet.Cell(r, 5).Value =
                            a.TotalWorkingHours?
                                .ToString(@"hh\:mm") ?? "-";

                        r++;
                    }

                    break;

                case "Leave":

                    var leaveRecords =
                        (List<Leave>)data;

                    sheet.Cell(1, 1).Value =
                        "Employee";

                    sheet.Cell(1, 2).Value =
                        "Type";

                    sheet.Cell(1, 3).Value =
                        "Start Date";

                    sheet.Cell(1, 4).Value =
                        "End Date";

                    sheet.Cell(1, 5).Value =
                        "Status";

                    int lr = 2;

                    foreach (var l in leaveRecords)
                    {
                        sheet.Cell(lr, 1).Value =
                            l.Employee != null
                                ? $"{l.Employee.FirstName} {l.Employee.LastName}"
                                : "";

                        sheet.Cell(lr, 2).Value =
                            l.LeaveType;

                        sheet.Cell(lr, 3).Value =
                            l.StartDate.ToShortDateString();

                        sheet.Cell(lr, 4).Value =
                            l.EndDate.ToShortDateString();

                        sheet.Cell(lr, 5).Value =
                            l.Status;

                        lr++;
                    }

                    break;

                case "Payroll":

                    var payrolls =
                        (List<PayRoll>)data;

                    sheet.Cell(1, 1).Value =
                        "Employee";

                    sheet.Cell(1, 2).Value =
                        "Basic Salary";

                    sheet.Cell(1, 3).Value =
                        "Bonus";

                    sheet.Cell(1, 4).Value =
                        "Deduction";

                    sheet.Cell(1, 5).Value =
                        "Net Salary";

                    sheet.Cell(1, 6).Value =
                        "Pay Date";

                    int pr = 2;

                    foreach (var p in payrolls)
                    {
                        sheet.Cell(pr, 1).Value =
                            p.Employee != null
                                ? $"{p.Employee.FirstName} {p.Employee.LastName}"
                                : "";

                        sheet.Cell(pr, 2).Value =
                            p.BasicSalary;

                        sheet.Cell(pr, 3).Value =
                            p.Bonus;

                        sheet.Cell(pr, 4).Value =
                            p.Deduction;

                        sheet.Cell(pr, 5).Value =
                            p.NetSalary;

                        sheet.Cell(pr, 6).Value =
                            p.PayDate.ToShortDateString();

                        pr++;
                    }

                    break;

                case "Departments":

                    var departments =
                        (List<Department>)data;

                    sheet.Cell(1, 1).Value =
                        "Name";

                    sheet.Cell(1, 2).Value =
                        "Description";

                    int dr = 2;

                    foreach (var d in departments)
                    {
                        sheet.Cell(dr, 1).Value =
                            d.Name;

                        sheet.Cell(dr, 2).Value =
                            d.Description;

                        dr++;
                    }

                    break;
            }

            sheet.Columns()
                .AdjustToContents();

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            var content =
                stream.ToArray();

            string fileName =
                $"{filter.ReportType}_Report_{DateTime.Now:yyyyMMdd}.xlsx";

            // Audit Log
            await _auditService.LogAsync(
                "Report",
                "ExportExcel",
                BuildReportLogDetails(
                    filter,
                    "exported as Excel")
            );

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // =====================================================
        // EXPORT PDF
        // =====================================================

        public async Task<IActionResult> ExportPdf(
            ReportFilterViewModel filter)
        {
            var data =
                await GetReportData(filter);

            var document =
                QuestPDF.Fluent.Document.Create(
                    container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);

                            page.Margin(30);

                            page.Header()
                                .Text(
                                    $"{filter.ReportType} Report")
                                .FontSize(20)
                                .Bold();

                            page.Footer()
                                .AlignCenter()
                                .Text(x =>
                                {
                                    x.Span(
                                        "Generated on ");

                                    x.Span(
                                        DateTime.Now
                                            .ToShortDateString());
                                });

                            page.Content()
                                .Table(table =>
                                {
                                    switch (
                                        filter.ReportType)
                                    {
                                        case "Employees":

                                            var employees =
                                                (List<Employee>)data;

                                            table.ColumnsDefinition(
                                                columns =>
                                                {
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                });

                                            table.Header(header =>
                                            {
                                                header.Cell()
                                                    .Text("Name")
                                                    .Bold();

                                                header.Cell()
                                                    .Text("Email")
                                                    .Bold();

                                                header.Cell()
                                                    .Text("Department")
                                                    .Bold();

                                                header.Cell()
                                                    .Text("Status")
                                                    .Bold();
                                            });

                                            foreach (
                                                var e in employees)
                                            {
                                                table.Cell()
                                                    .Text(
                                                        $"{e.FirstName} {e.LastName}");

                                                table.Cell()
                                                    .Text(e.Email);

                                                table.Cell()
                                                    .Text(
                                                        e.Department?.Name
                                                        ?? "-");

                                                table.Cell()
                                                    .Text(e.Status);
                                            }

                                            break;

                                        case "Payroll":

                                            var payrolls =
                                                (List<PayRoll>)data;

                                            table.ColumnsDefinition(
                                                columns =>
                                                {
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                });

                                            table.Header(header =>
                                            {
                                                header.Cell()
                                                    .Text("Employee")
                                                    .Bold();

                                                header.Cell()
                                                    .Text("Net Salary")
                                                    .Bold();

                                                header.Cell()
                                                    .Text("Pay Date")
                                                    .Bold();

                                                header.Cell()
                                                    .Text("Bonus/Deduction")
                                                    .Bold();
                                            });

                                            foreach (
                                                var p in payrolls)
                                            {
                                                table.Cell()
                                                    .Text(
                                                        p.Employee != null
                                                            ? $"{p.Employee.FirstName} {p.Employee.LastName}"
                                                            : "-");

                                                table.Cell()
                                                    .Text(
                                                        p.NetSalary
                                                            .ToString("N2"));

                                                table.Cell()
                                                    .Text(
                                                        p.PayDate
                                                            .ToShortDateString());

                                                table.Cell()
                                                    .Text(
                                                        $"+{p.Bonus:N2} / -{p.Deduction:N2}");
                                            }

                                            break;

                                        default:

                                            table.ColumnsDefinition(
                                                columns =>
                                                    columns.RelativeColumn());

                                            table.Cell()
                                                .Text(
                                                    "No data available for this report type in PDF yet.");

                                            break;
                                    }
                                });
                        });
                    });

            var pdfBytes =
                document.GeneratePdf();

            string fileName =
                $"{filter.ReportType}_Report_{DateTime.Now:yyyyMMdd}.pdf";

            // Audit Log
            await _auditService.LogAsync(
                "Report",
                "ExportPdf",
                BuildReportLogDetails(
                    filter,
                    "exported as PDF")
            );

            return File(
                pdfBytes,
                "application/pdf",
                fileName);
        }

        // =====================================================
        // REPORT AUDIT DETAILS
        // =====================================================

        private string BuildReportLogDetails(
            ReportFilterViewModel filter,
            string operation)
        {
            string details =
                $"User {User.Identity?.Name ?? "Unknown"} " +
                $"{operation} a {filter.ReportType} report.";

            if (filter.DepartmentId.HasValue)
            {
                details +=
                    $" Department ID: {filter.DepartmentId.Value}.";
            }

            if (filter.EmployeeId.HasValue)
            {
                details +=
                    $" Employee ID: {filter.EmployeeId.Value}.";
            }

            if (filter.FromDate.HasValue)
            {
                details +=
                    $" From: {filter.FromDate.Value:dd-MM-yyyy}.";
            }

            if (filter.ToDate.HasValue)
            {
                details +=
                    $" To: {filter.ToDate.Value:dd-MM-yyyy}.";
            }

            return details;
        }
    }
}