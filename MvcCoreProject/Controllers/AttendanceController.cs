using CoreProject.Models;
using CoreProject.Services;
using CoreProject.Services.IService;
using CoreProject.ViewModels;
using CoreProject.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AttendanceController> _logger;
        private readonly ITimezoneService _timezoneService;

        public AttendanceController(
            IAttendanceService attendanceService,
            UserManager<ApplicationUser> userManager,
            ILogger<AttendanceController> logger,
            ITimezoneService timezoneService)
        {
            _attendanceService = attendanceService;
            _userManager = userManager;
            _logger = logger;
            _timezoneService = timezoneService;
        }

        // GET: Attendance/Index - My Attendance (Employee's own attendance history)
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Default to last 30 days if no dates provided
                var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow.Date;

                var attendances = await _attendanceService.GetMyAttendanceAsync(userId, start, end);
                var summary = await _attendanceService.GetMyAttendanceSummaryAsync(userId, start, end);

                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");
                ViewBag.Summary = summary;
                ViewBag.UserName = user.DisplayName;

                return View(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading my attendance");
                TempData["Error"] = "Failed to load attendance records.";
                return View(Enumerable.Empty<AttendanceViewModel>());
            }
        }

        // GET: Attendance/CheckIn - Check In/Out page
        [HttpGet]
        public async Task<IActionResult> CheckIn()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _userManager.Users
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var todayStatus = await _attendanceService.GetTodayAttendanceStatusAsync(userId);

                // Get branch timezone
                var branchTimezone = user.Branch?.TimeZone ?? 0;
                var branchNow = _timezoneService.GetBranchNow(branchTimezone);
                var timezoneName = TimezoneHelper.GetTimezoneName(branchTimezone);

                ViewBag.UserName = user.DisplayName;
                ViewBag.CurrentTime = branchNow.ToString("HH:mm");
                ViewBag.CurrentDate = branchNow.ToString("dddd, MMMM dd, yyyy");
                ViewBag.TimezoneName = timezoneName;
                ViewBag.BranchTimezone = branchTimezone;

                return View(todayStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading check-in page");
                TempData["Error"] = "Failed to load check-in page.";
                return View();
            }
        }

        // POST: Attendance/PerformCheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformCheckIn()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _attendanceService.CheckInAsync(userId);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                }
                else
                {
                    TempData["Error"] = result.Message;
                }

                return RedirectToAction(nameof(CheckIn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing check-in");
                TempData["Error"] = "Failed to check in. Please try again.";
                return RedirectToAction(nameof(CheckIn));
            }
        }

        // POST: Attendance/PerformCheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformCheckOut()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _attendanceService.CheckOutAsync(userId);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                }
                else
                {
                    TempData["Error"] = result.Message;
                }

                return RedirectToAction(nameof(CheckIn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing check-out");
                TempData["Error"] = "Failed to check out. Please try again.";
                return RedirectToAction(nameof(CheckIn));
            }
        }

        // GET: Attendance/TeamAttendance - For Managers/HR/Admin to see team attendance
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> TeamAttendance(DateTime? date, DateTime? startDate, DateTime? endDate, string view = "daily")
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.View = view;
                ViewBag.UserName = user.DisplayName;

                if (view == "range")
                {
                    var start = startDate ?? DateTime.UtcNow.Date.AddDays(-7);
                    var end = endDate ?? DateTime.UtcNow.Date;

                    var attendances = await _attendanceService.GetTeamAttendanceRangeAsync(User, start, end);

                    ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                    ViewBag.EndDate = end.ToString("yyyy-MM-dd");

                    return View(attendances);
                }
                else
                {
                    var selectedDate = date ?? DateTime.UtcNow.Date;
                    var attendances = await _attendanceService.GetTeamAttendanceAsync(User, selectedDate);

                    ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
                    ViewBag.SelectedDateDisplay = selectedDate.ToString("dddd, MMMM dd, yyyy");

                    return View(attendances);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading team attendance");
                TempData["Error"] = "Failed to load team attendance.";
                return View(Enumerable.Empty<TeamAttendanceViewModel>());
            }
        }

        // GET: Attendance/Reports - Attendance reports and analytics
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate, int? userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Default to current month if no dates provided
                var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var end = endDate ?? DateTime.UtcNow.Date;

                var report = await _attendanceService.GetAttendanceReportAsync(User, start, end, userId);

                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");
                ViewBag.SelectedUserId = userId;
                ViewBag.UserName = currentUser.DisplayName;

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading attendance reports");
                TempData["Error"] = "Failed to load reports.";
                return View(new AttendanceReportViewModel
                {
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date,
                    Summary = new AttendanceSummaryViewModel(),
                    Attendances = new List<AttendanceViewModel>()
                });
            }
        }

        // GET: Attendance/UserReport - User-specific attendance report with date range
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> UserReport(int userId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                _logger.LogInformation("UserReport requested for userId: {UserId}", userId);

                // Check if userId is valid
                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid userId parameter: {UserId}", userId);
                    TempData["Error"] = "Invalid user ID.";
                    return RedirectToAction(nameof(Reports));
                }

                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Load current user with Branch to check permissions
                var currentUser = await _userManager.Users
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

                // Load target user with related entities (Department and Branch)
                // For Admin and HR in main branch, ignore query filters to access all users
                IQueryable<ApplicationUser> targetUserQuery = _userManager.Users
                    .Include(u => u.Department)
                    .Include(u => u.Branch);

                // Admin can access all users - ignore branch filters
                if (User.IsInRole("Admin") || (User.IsInRole("HR") && currentUser?.Branch?.IsMainBranch == true))
                {
                    targetUserQuery = targetUserQuery.IgnoreQueryFilters();
                }

                var targetUser = await targetUserQuery.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (targetUser == null)
                {
                    // Log all available user IDs for debugging (ignore filters to show all)
                    var allUserIds = await _userManager.Users.IgnoreQueryFilters()
                        .Where(u => u.IsActive)
                        .Select(u => u.Id)
                        .ToListAsync();
                    _logger.LogWarning("User not found with ID: {UserId}. Available active user IDs: {UserIds}",
                        userId, string.Join(", ", allUserIds));
                    TempData["Error"] = $"User not found (ID: {userId}). User may be inactive.";
                    return RedirectToAction(nameof(Reports));
                }

                // Authorization check: Verify current user has permission to view this user's report
                if (!User.IsInRole("Admin"))
                {
                    // HR in main branch can access all employees
                    if (User.IsInRole("HR") && currentUser?.Branch?.IsMainBranch == true)
                    {
                        // Allow access
                    }
                    // HR in non-main branch can only access employees in their branch
                    else if (User.IsInRole("HR") && targetUser.BranchID != currentUser?.BranchID)
                    {
                        TempData["Error"] = "You don't have permission to view this user's attendance report.";
                        return RedirectToAction(nameof(Reports));
                    }
                    // Manager can only access their subordinates
                    else if (User.IsInRole("Manager"))
                    {
                        var isSubordinate = await _userManager.Users
                            .AnyAsync(u => u.Id == userId && u.ManagerID == currentUserId);

                        if (!isSubordinate && userId != currentUserId)
                        {
                            TempData["Error"] = "You don't have permission to view this user's attendance report.";
                            return RedirectToAction(nameof(Reports));
                        }
                    }
                }

                // Default to current month if no dates provided
                var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var end = endDate ?? DateTime.UtcNow.Date;

                var report = await _attendanceService.GetUserAttendanceReportAsync(userId, start, end);

                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");
                ViewBag.UserId = userId;
                ViewBag.UserName = targetUser.DisplayName ?? "Unknown User";
                ViewBag.UserDepartment = targetUser.Department?.Name ?? "N/A";
                ViewBag.UserBranch = targetUser.Branch?.Name ?? "N/A";

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user attendance report for user {UserId}", userId);
                TempData["Error"] = "Failed to load user attendance report.";
                return RedirectToAction(nameof(Reports));
            }
        }

        // GET: Attendance/PendingHRPosts - View pending HR posts
        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> PendingHRPosts()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(TeamAttendance));
                }

                var pendingPosts = await _attendanceService.GetPendingHRPostsAsync(user.BranchID);

                ViewBag.UserName = user.DisplayName;
                ViewBag.BranchName = user.Branch?.Name ?? "Unknown Branch";

                return View(pendingPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending HR posts");
                TempData["Error"] = "Failed to load pending HR posts.";
                return View(Enumerable.Empty<AttendanceViewModel>());
            }
        }

        // POST: Attendance/PostToHR
        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostToHR(int attendanceId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _attendanceService.PostToHRAsync(attendanceId, userId);

                if (result)
                {
                    TempData["Success"] = "Attendance posted to HR successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to post attendance to HR.";
                }

                return RedirectToAction(nameof(PendingHRPosts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting attendance to HR");
                TempData["Error"] = "Failed to post attendance to HR.";
                return RedirectToAction(nameof(PendingHRPosts));
            }
        }

        // GET: Attendance/RecalculateDurations - Fix existing attendance records with zero duration
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateDurations()
        {
            try
            {
                var updatedCount = await _attendanceService.RecalculateAllDurationsAsync();
                TempData["Success"] = $"Successfully recalculated {updatedCount} attendance records.";
                return RedirectToAction(nameof(Reports));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating durations");
                TempData["Error"] = "Failed to recalculate durations.";
                return RedirectToAction(nameof(Reports));
            }
        }

        // GET: Attendance/BranchAttendance - View attendance by branch
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> BranchAttendance(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var model = await _attendanceService.GetBranchAttendanceAsync(User, startDate, endDate);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branch attendance");
                TempData["Error"] = "Failed to load branch attendance.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: Attendance/ExportReport - Export attendance report as CSV
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> ExportReport(DateTime? startDate, DateTime? endDate, int? userId)
        {
            try
            {
                var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var end = endDate ?? DateTime.UtcNow.Date;

                _logger.LogInformation("Exporting attendance report: Start={Start}, End={End}, UserId={UserId}", start, end, userId);

                var report = await _attendanceService.GetAttendanceReportAsync(User, start, end, userId);

                _logger.LogInformation("Report retrieved with {Count} attendance records", report.Attendances?.Count() ?? 0);

                if (report.Attendances == null || !report.Attendances.Any())
                {
                    _logger.LogWarning("No attendance records found for export");
                    TempData["Error"] = "No attendance records found for the selected period.";
                    return RedirectToAction(nameof(Reports), new { startDate, endDate, userId });
                }

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("User Name,Date,Day,First Check In,Last Check Out,Duration (Hours),Status,Minutes Late/Early,HR Posted,HR Posted By,HR Posted Date");

                foreach (var attendance in report.Attendances)
                {
                    var durationHours = attendance.Duration > 0 ? $"{attendance.Duration / 60.0:F2}" : "0.00";
                    var hrPosted = attendance.HRPosted ? "Yes" : "No";
                    var hrUserName = attendance.HRUserName ?? "";
                    var hrPostedDate = attendance.HRPostedDate?.ToString("yyyy-MM-dd HH:mm") ?? "";
                    var dayOfWeek = attendance.Date.DayOfWeek.ToString();
                    var minutesLate = attendance.MinutesLate.HasValue ? attendance.MinutesLate.Value.ToString() : "";

                    csv.AppendLine($"\"{attendance.UserName ?? "Unknown"}\",\"{attendance.Date:yyyy-MM-dd}\",\"{dayOfWeek}\",\"{attendance.FirstCheckIn ?? ""}\",\"{attendance.LastCheckOut ?? ""}\",\"{durationHours}\",\"{attendance.Status}\",\"{minutesLate}\",\"{hrPosted}\",\"{hrUserName}\",\"{hrPostedDate}\"");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"Attendance_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";

                _logger.LogInformation("CSV file generated: {FileName}, Size: {Size} bytes", fileName, bytes.Length);

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance report");
                TempData["Error"] = $"Failed to export report: {ex.Message}";
                return RedirectToAction(nameof(Reports), new { startDate, endDate, userId });
            }
        }

        // GET: Attendance/ExportReportPdf - Export attendance report as PDF
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> ExportReportPdf(DateTime? startDate, DateTime? endDate, int? userId)
        {
            try
            {
                var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var end = endDate ?? DateTime.UtcNow.Date;

                _logger.LogInformation("Exporting attendance report as PDF: Start={Start}, End={End}, UserId={UserId}", start, end, userId);

                var report = await _attendanceService.GetAttendanceReportAsync(User, start, end, userId);

                _logger.LogInformation("Report retrieved with {Count} attendance records", report.Attendances?.Count() ?? 0);

                if (report.Attendances == null || !report.Attendances.Any())
                {
                    _logger.LogWarning("No attendance records found for export");
                    TempData["Error"] = "No attendance records found for the selected period.";
                    return RedirectToAction(nameof(Reports), new { startDate, endDate, userId });
                }

                // Get user name if userId is provided
                string? userName = null;
                if (userId.HasValue)
                {
                    var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                    userName = user?.DisplayName;
                }

                // Generate PDF
                var pdfService = new CoreProject.Services.PdfExportService();
                var pdfBytes = pdfService.GenerateAttendanceReportPdf(report.Attendances, start, end, userName);

                var fileName = $"Attendance_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";

                _logger.LogInformation("PDF file generated: {FileName}, Size: {Size} bytes", fileName, pdfBytes.Length);

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance report as PDF");
                TempData["Error"] = $"Failed to export PDF report: {ex.Message}";
                return RedirectToAction(nameof(Reports), new { startDate, endDate, userId });
            }
        }

        // GET: Attendance/ExportBranchAttendanceCsv - Export branch attendance as CSV
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> ExportBranchAttendanceCsv(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.Date;
                var end = endDate ?? DateTime.UtcNow.Date;

                _logger.LogInformation("Exporting branch attendance as CSV: Start={Start}, End={End}", start, end);

                var model = await _attendanceService.GetBranchAttendanceAsync(User, start, end);

                if (model.Branches == null || !model.Branches.Any())
                {
                    _logger.LogWarning("No branch attendance data found for export");
                    TempData["Error"] = "No attendance data found for the selected period.";
                    return RedirectToAction(nameof(BranchAttendance), new { startDate, endDate });
                }

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Branch,User Name,Email,Department,Check In,Check Out,Duration (Hours),Status");

                foreach (var branch in model.Branches)
                {
                    foreach (var user in branch.Users)
                    {
                        var durationHours = user.Duration > 0 ? $"{user.Duration / 60.0:F2}" : "0.00";
                        csv.AppendLine($"\"{branch.BranchName}\",\"{user.UserName}\",\"{user.Email}\",\"{user.Department}\",\"{user.FirstCheckIn ?? ""}\",\"{user.LastCheckOut ?? ""}\",\"{durationHours}\",\"{user.Status}\"");
                    }
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"Branch_Attendance_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";

                _logger.LogInformation("CSV file generated: {FileName}, Size: {Size} bytes", fileName, bytes.Length);

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting branch attendance as CSV");
                TempData["Error"] = $"Failed to export CSV: {ex.Message}";
                return RedirectToAction(nameof(BranchAttendance), new { startDate, endDate });
            }
        }

        // GET: Attendance/ExportBranchAttendancePdf - Export branch attendance as PDF
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> ExportBranchAttendancePdf(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.Date;
                var end = endDate ?? DateTime.UtcNow.Date;

                _logger.LogInformation("Exporting branch attendance as PDF: Start={Start}, End={End}", start, end);

                var model = await _attendanceService.GetBranchAttendanceAsync(User, start, end);

                if (model.Branches == null || !model.Branches.Any())
                {
                    _logger.LogWarning("No branch attendance data found for export");
                    TempData["Error"] = "No attendance data found for the selected period.";
                    return RedirectToAction(nameof(BranchAttendance), new { startDate, endDate });
                }

                // Generate PDF
                var pdfService = new CoreProject.Services.PdfExportService();
                var pdfBytes = pdfService.GenerateBranchAttendanceReportPdf(model);

                var fileName = $"Branch_Attendance_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";

                _logger.LogInformation("PDF file generated: {FileName}, Size: {Size} bytes", fileName, pdfBytes.Length);

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting branch attendance as PDF");
                TempData["Error"] = $"Failed to export PDF: {ex.Message}";
                return RedirectToAction(nameof(BranchAttendance), new { startDate, endDate });
            }
        }
    }
}
