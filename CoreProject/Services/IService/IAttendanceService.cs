using CoreProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IAttendanceService
    {
        // Check In/Out
        Task<CheckInOutResultViewModel> CheckInAsync(int userId);
        Task<CheckInOutResultViewModel> CheckOutAsync(int userId);
        Task<TodayAttendanceViewModel?> GetTodayAttendanceStatusAsync(int userId);

        // My Attendance
        Task<IEnumerable<AttendanceViewModel>> GetMyAttendanceAsync(int userId, DateTime? startDate, DateTime? endDate);
        Task<AttendanceSummaryViewModel> GetMyAttendanceSummaryAsync(int userId, DateTime startDate, DateTime endDate);

        // Team Attendance (Manager/HR)
        Task<IEnumerable<TeamAttendanceViewModel>> GetTeamAttendanceAsync(ClaimsPrincipal currentUser, DateTime date);
        Task<IEnumerable<TeamAttendanceViewModel>> GetTeamAttendanceRangeAsync(ClaimsPrincipal currentUser, DateTime startDate, DateTime endDate);

        // Reports
        Task<AttendanceReportViewModel> GetAttendanceReportAsync(ClaimsPrincipal currentUser, DateTime startDate, DateTime endDate, int? userId = null);
        Task<AttendanceReportViewModel> GetUserAttendanceReportAsync(int userId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<AttendanceViewModel>> GetPendingHRPostsAsync(int branchId);
        Task<bool> PostToHRAsync(int attendanceId, int hrUserId);

        // Branch Attendance
        Task<BranchAttendanceViewModel> GetBranchAttendanceAsync(ClaimsPrincipal currentUser, DateTime? startDate = null, DateTime? endDate = null);

        // Utility
        Task<int> RecalculateAllDurationsAsync();

        // Manual Attendance Management (Admin/HR)
        Task<IEnumerable<AttendanceViewModel>> GetUsersForManualAttendanceAsync(ClaimsPrincipal currentUser, int? branchId = null);
        Task<AttendanceViewModel?> GetAttendanceByIdAsync(int attendanceId);
        Task<(bool Success, string Message)> CreateManualAttendanceAsync(int userId, DateTime date, string? checkInTime, string? checkOutTime, int createdByUserId);
        Task<(bool Success, string Message)> UpdateManualAttendanceAsync(int attendanceId, string? checkInTime, string? checkOutTime, int updatedByUserId);
        Task<(bool Success, string Message)> ManualCheckInAsync(int userId, DateTime date, string checkInTime, int performedByUserId);
        Task<(bool Success, string Message)> ManualCheckOutAsync(int attendanceId, string checkOutTime, int performedByUserId);
    }
}
