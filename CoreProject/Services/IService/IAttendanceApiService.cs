using CoreProject.Utilities.DTOs;
using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    /// <summary>
    /// Service interface for mobile app attendance API
    /// Handles check-in and check-out operations with all validations
    /// </summary>
    public interface IAttendanceApiService
    {
        /// <summary>
        /// Processes attendance action (check-in or check-out) for a user
        /// Validates user, device, branch assignment, UDID, and timezone
        /// Calculates status (on time/late) and duration
        /// </summary>
        /// <param name="request">Attendance action request containing username, UDID, device ID, and action type</param>
        /// <returns>Attendance action response with success status, message, and attendance data</returns>
        Task<AttendanceActionResponseDto> ProcessAttendanceActionAsync(AttendanceActionRequestDto request);

        /// <summary>
        /// Gets attendance report for the authenticated user
        /// Returns daily attendance records with check-in, check-out, duration, and status
        /// Includes vacation days from assign table
        /// </summary>
        /// <param name="userId">User ID from JWT token</param>
        /// <param name="request">Optional date range (defaults to current month)</param>
        /// <returns>Attendance report with daily records and summary statistics</returns>
        Task<AttendanceReportResponseDto> GetUserAttendanceReportAsync(int userId, AttendanceReportRequestDto request);

        /// <summary>
        /// Processes passthrough access control verification
        /// Validates user, device, UDID, and checks if device belongs to user's branch
        /// Verifies that AccessControlState is 1 for the device
        /// </summary>
        /// <param name="request">Passthrough request containing username, UDID, and device ID</param>
        /// <returns>Passthrough response with access granted/denied status</returns>
        Task<PassthroughResponseDto> ProcessPassthroughAsync(PassthroughRequestDto request);

        /// <summary>
        /// Checks the current attendance status of the authenticated user
        /// Returns if user is checked in, checked out, or hasn't checked in yet for today
        /// </summary>
        /// <param name="userId">User ID from JWT token</param>
        /// <returns>User status response with current attendance state</returns>
        Task<UserStatusResponseDto> CheckUserStatusAsync(int userId);

        /// <summary>
        /// Gets the current user profile data (same as login but without token)
        /// Returns updated user information including organization, branch, department, and timetable
        /// </summary>
        /// <param name="userId">User ID from JWT token</param>
        /// <returns>User profile response with complete user data</returns>
        Task<UserProfileResponseDto> GetUserProfileAsync(int userId);
    }
}
