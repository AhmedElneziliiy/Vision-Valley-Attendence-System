using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreProject.Utilities.DTOs
{
    #region Request DTOs

    /// <summary>
    /// Request model for getting user attendance report
    /// </summary>
    public class AttendanceReportRequestDto
    {
        /// <summary>
        /// Start date for the report (optional, defaults to first day of current month)
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// End date for the report (optional, defaults to today)
        /// </summary>
        public DateTime? DateTo { get; set; }
    }

    /// <summary>
    /// Check-in/Check-out request model for mobile app attendance
    /// </summary>
    public class AttendanceActionRequestDto
    {
        /// <summary>
        /// User email or username for identification
        /// </summary>
        [Required(ErrorMessage = "Username is required")]
        [EmailAddress(ErrorMessage = "Username must be a valid email address")]
        public string Username { get; set; } = null!;

        /// <summary>
        /// Unique Device Identifier (UDID) for device verification
        /// </summary>
        [Required(ErrorMessage = "UDID is required")]
        [MinLength(10, ErrorMessage = "Invalid UDID format")]
        public string UDID { get; set; } = null!;

        /// <summary>
        /// Device ID from the Devices table
        /// </summary>
        [Required(ErrorMessage = "DeviceID is required")]
        public string DeviceID { get; set; } = null!;

        /// <summary>
        /// Action type: "CheckIn" or "CheckOut"
        /// </summary>
        [Required(ErrorMessage = "ActionType is required")]
        [RegularExpression("^(CheckIn|CheckOut)$", ErrorMessage = "ActionType must be either 'CheckIn' or 'CheckOut'")]
        public string ActionType { get; set; } = null!;
    }

    /// <summary>
    /// Passthrough request model for access control verification
    /// </summary>
    public class PassthroughRequestDto
    {
        /// <summary>
        /// User email or username for identification
        /// </summary>
        [Required(ErrorMessage = "Username is required")]
        [EmailAddress(ErrorMessage = "Username must be a valid email address")]
        public string Username { get; set; } = null!;

        /// <summary>
        /// Unique Device Identifier (UDID) for device verification
        /// </summary>
        [Required(ErrorMessage = "UDID is required")]
        [MinLength(10, ErrorMessage = "Invalid UDID format")]
        public string UDID { get; set; } = null!;

        /// <summary>
        /// Device ID from the Devices table
        /// </summary>
        [Required(ErrorMessage = "DeviceID is required")]
        public string DeviceID { get; set; } = null!;
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Response model for passthrough access control verification
    /// </summary>
    public class PassthroughResponseDto
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result
        /// </summary>
        public string Message { get; set; } = null!;

        /// <summary>
        /// Indicates if access is granted
        /// </summary>
        public bool AccessGranted { get; set; }

        /// <summary>
        /// Current access control state of the device
        /// </summary>
        public int? AccessControlState { get; set; }

        /// <summary>
        /// User information
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Branch name where device is located
        /// </summary>
        public string? BranchName { get; set; }
    }

    /// <summary>
    /// Response model for attendance check-in/check-out operations
    /// </summary>
    public class AttendanceActionResponseDto
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result
        /// </summary>
        public string Message { get; set; } = null!;

        /// <summary>
        /// Attendance data if operation was successful
        /// </summary>
        public AttendanceDataDto? Data { get; set; }
    }

    /// <summary>
    /// Attendance record data returned after check-in/check-out
    /// </summary>
    public class AttendanceDataDto
    {
        /// <summary>
        /// Attendance record ID
        /// </summary>
        public int AttendanceId { get; set; }

        /// <summary>
        /// User ID who performed the action
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// User display name
        /// </summary>
        public string UserName { get; set; } = null!;

        /// <summary>
        /// Action type performed (CheckIn/CheckOut)
        /// </summary>
        public string ActionType { get; set; } = null!;

        /// <summary>
        /// Date of the attendance record
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Time of the action (in user's local timezone)
        /// </summary>
        public TimeSpan ActionTime { get; set; }

        /// <summary>
        /// Timestamp of the action (UTC)
        /// </summary>
        public DateTime ActionTimestamp { get; set; }

        /// <summary>
        /// Attendance status (On Time, Late, etc.)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Check-in time (for check-out records)
        /// </summary>
        public TimeSpan? CheckInTime { get; set; }

        /// <summary>
        /// Check-out time (for check-out records)
        /// </summary>
        public TimeSpan? CheckOutTime { get; set; }

        /// <summary>
        /// Duration of work in minutes (for check-out records)
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Duration formatted as HH:mm (for check-out records)
        /// </summary>
        public string? DurationFormatted { get; set; }

        /// <summary>
        /// Expected check-in time from timetable
        /// </summary>
        public TimeSpan? ExpectedCheckInTime { get; set; }

        /// <summary>
        /// Expected check-out time from timetable
        /// </summary>
        public TimeSpan? ExpectedCheckOutTime { get; set; }

        /// <summary>
        /// Device ID used for the action
        /// </summary>
        public string DeviceId { get; set; } = null!;

        /// <summary>
        /// Branch name where action was performed
        /// </summary>
        public string BranchName { get; set; } = null!;

        /// <summary>
        /// Organization name
        /// </summary>
        public string OrganizationName { get; set; } = null!;

        /// <summary>
        /// User's timezone
        /// </summary>
        public string Timezone { get; set; } = null!;
    }

    /// <summary>
    /// Response model for attendance report
    /// </summary>
    public class AttendanceReportResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public AttendanceReportDataDto? Data { get; set; }
    }

    /// <summary>
    /// Attendance report data container
    /// </summary>
    public class AttendanceReportDataDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int TotalWorkingDays { get; set; }
        public int TotalPresentDays { get; set; }
        public int TotalAbsentDays { get; set; }
        public int TotalVacationDays { get; set; }
        public int TotalLateDays { get; set; }
        public int TotalEarlyDays { get; set; }
        public List<DailyAttendanceDto> DailyRecords { get; set; } = new List<DailyAttendanceDto>();
    }

    /// <summary>
    /// Daily attendance record for report
    /// </summary>
    public class DailyAttendanceDto
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = null!;
        public bool IsVacation { get; set; }
        public string? VacationName { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string? DurationFormatted { get; set; }
        public string Status { get; set; } = null!; // Present, Absent, On Time, Late, Very Late, Early, Vacation
        public int? MinutesLate { get; set; }
        public TimeSpan? ExpectedCheckInTime { get; set; }
        public TimeSpan? ExpectedCheckOutTime { get; set; }
    }

    #endregion
}
