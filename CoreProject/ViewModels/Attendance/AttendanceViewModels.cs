using System;
using System.Collections.Generic;
using CoreProject.Models;

namespace CoreProject.ViewModels
{
    // For displaying attendance records
    public class AttendanceViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? BranchName { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime Date { get; set; }
        public string? FirstCheckIn { get; set; }
        public string? LastCheckOut { get; set; }
        public int Duration { get; set; } // minutes
        public bool HRPosted { get; set; }
        public string? HRUserName { get; set; }
        public DateTime? HRPostedDate { get; set; }
        public List<AttendanceRecordViewModel> Records { get; set; } = new();

        // New fields for timetable status
        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Absent;
        public int? MinutesLate { get; set; }

        // Computed properties
        public string DurationDisplay => $"{Duration / 60}h {Duration % 60}m";

        public string Status => GetStatus();
        public string StatusClass => GetStatusClass();

        // New computed properties for timetable status
        public string AttendanceStatusDisplay => GetAttendanceStatusDisplay();
        public string AttendanceStatusClass => GetAttendanceStatusClass();
        public string LateMinutesDisplay => GetLateMinutesDisplay();

        private string GetStatus()
        {
            if (string.IsNullOrEmpty(FirstCheckIn)) return "Absent";
            if (string.IsNullOrEmpty(LastCheckOut)) return "Checked In";
            return "Present";
        }

        private string GetStatusClass()
        {
            if (string.IsNullOrEmpty(FirstCheckIn)) return "bg-danger";
            if (string.IsNullOrEmpty(LastCheckOut)) return "bg-warning";
            return "bg-success";
        }

        private string GetAttendanceStatusDisplay()
        {
            return AttendanceStatus switch
            {
                AttendanceStatus.OnTime => "On Time",
                AttendanceStatus.Late => "Late",
                AttendanceStatus.VeryLate => "Very Late",
                AttendanceStatus.Early => "Early",
                AttendanceStatus.Absent => "Absent",
                _ => "Unknown"
            };
        }

        private string GetAttendanceStatusClass()
        {
            return AttendanceStatus switch
            {
                AttendanceStatus.OnTime => "success",
                AttendanceStatus.Late => "warning",
                AttendanceStatus.VeryLate => "danger",
                AttendanceStatus.Early => "info",
                AttendanceStatus.Absent => "secondary",
                _ => "secondary"
            };
        }

        private string GetLateMinutesDisplay()
        {
            if (!MinutesLate.HasValue || MinutesLate == 0) return "";

            if (MinutesLate > 0)
                return $"+{MinutesLate}min late";
            else
                return $"{Math.Abs(MinutesLate.Value)}min early";
        }
    }

    // For individual check records
    public class AttendanceRecordViewModel
    {
        public int Id { get; set; }
        public TimeSpan Time { get; set; }
        public bool IsCheckIn { get; set; }
        public bool? IsAutomated { get; set; }
        public int? FaceValidation { get; set; } // 1 = success, 0 = fail
        public string? ReasonName { get; set; }

        public string TypeDisplay => IsCheckIn ? "Check In" : "Check Out";
        public string TypeClass => IsCheckIn ? "success" : "danger";
        public string TimeDisplay => Time.ToString(@"hh\:mm\:ss");
    }

    // For today's attendance status (Check In/Out page)
    public class TodayAttendanceViewModel
    {
        public int? AttendanceId { get; set; }
        public DateTime Date { get; set; }
        public string? FirstCheckIn { get; set; }
        public string? LastCheckOut { get; set; }
        public int Duration { get; set; }
        public bool IsCheckedIn { get; set; }
        public List<AttendanceRecordViewModel> Records { get; set; } = new();

        // New fields for timetable status
        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Absent;
        public int? MinutesLate { get; set; }

        public string DurationDisplay => $"{Duration / 60}h {Duration % 60}m";
        public string StatusMessage => IsCheckedIn ? "You are currently checked in" : "You are currently checked out";
        public string NextAction => IsCheckedIn ? "Check Out" : "Check In";

        // New computed properties
        public string AttendanceStatusDisplay => GetAttendanceStatusDisplay();
        public string AttendanceStatusClass => GetAttendanceStatusClass();
        public string LateMinutesDisplay => GetLateMinutesDisplay();

        private string GetAttendanceStatusDisplay()
        {
            return AttendanceStatus switch
            {
                AttendanceStatus.OnTime => "On Time",
                AttendanceStatus.Late => "Late",
                AttendanceStatus.VeryLate => "Very Late",
                AttendanceStatus.Early => "Early",
                AttendanceStatus.Absent => "Not Checked In",
                _ => "Unknown"
            };
        }

        private string GetAttendanceStatusClass()
        {
            return AttendanceStatus switch
            {
                AttendanceStatus.OnTime => "success",
                AttendanceStatus.Late => "warning",
                AttendanceStatus.VeryLate => "danger",
                AttendanceStatus.Early => "info",
                AttendanceStatus.Absent => "secondary",
                _ => "secondary"
            };
        }

        private string GetLateMinutesDisplay()
        {
            if (!MinutesLate.HasValue || MinutesLate == 0) return "";

            if (MinutesLate > 0)
                return $"(+{MinutesLate} min)";
            else
                return $"({Math.Abs(MinutesLate.Value)} min early)";
        }
    }

    // Result of check-in/out operation
    public class CheckInOutResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public bool IsCheckIn { get; set; }
        public TimeSpan Time { get; set; }
        public int? AttendanceId { get; set; }
    }

    // Team attendance (for managers)
    public class TeamAttendanceViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Department { get; set; } = null!;
        public DateTime Date { get; set; }
        public string? FirstCheckIn { get; set; }
        public string? LastCheckOut { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = null!;
        public bool HRPosted { get; set; }

        public string DurationDisplay => $"{Duration / 60}h {Duration % 60}m";
        public string StatusClass => Status == "Absent" ? "danger" : Status == "Checked In" ? "warning" : "success";
    }

    // Summary statistics
    public class AttendanceSummaryViewModel
    {
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int TotalMinutes { get; set; }
        public int AverageMinutes { get; set; }

        public string TotalHours => $"{TotalMinutes / 60}h {TotalMinutes % 60}m";
        public string AverageHours => $"{AverageMinutes / 60}h {AverageMinutes % 60}m";
        public decimal AttendanceRate => TotalDays > 0 ? ((decimal)PresentDays / TotalDays * 100) : 0;
    }

    // For reports
    public class AttendanceReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AttendanceSummaryViewModel Summary { get; set; } = null!;
        public List<AttendanceViewModel> Attendances { get; set; } = new();
        public List<DailyAttendanceStats> DailyStats { get; set; } = new();
    }

    public class DailyAttendanceStats
    {
        public DateTime Date { get; set; }
        public int TotalEmployees { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }

        public decimal AttendanceRate => TotalEmployees > 0 ? ((decimal)PresentCount / TotalEmployees * 100) : 0;
    }

    // For branch attendance view
    public class BranchAttendanceViewModel
    {
        public List<BranchAttendanceData> Branches { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsToday { get; set; }
        public bool CanViewAllBranches { get; set; }
        public int? UserBranchId { get; set; }
    }

    public class BranchAttendanceData
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public bool IsMainBranch { get; set; }
        public int TotalUsers { get; set; }
        public int PresentUsers { get; set; }
        public int AbsentUsers { get; set; }
        public int CheckedInUsers { get; set; } // Still checked in (no checkout yet)
        public List<UserAttendanceData> Users { get; set; } = new();

        public decimal AttendanceRate => TotalUsers > 0 ? ((decimal)PresentUsers / TotalUsers * 100) : 0;
        public string AttendanceRateDisplay => $"{AttendanceRate:F1}%";
    }

    public class UserAttendanceData
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Department { get; set; } = null!;
        public string? FirstCheckIn { get; set; }
        public string? LastCheckOut { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = null!;
        public bool HRPosted { get; set; }

        public string DurationDisplay => $"{Duration / 60}h {Duration % 60}m";
        public string StatusClass => Status == "Absent" ? "danger" : Status == "Checked In" ? "warning" : "success";
    }
}
