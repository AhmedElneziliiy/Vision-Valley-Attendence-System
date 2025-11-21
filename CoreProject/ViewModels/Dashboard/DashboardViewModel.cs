using System;
using System.Collections.Generic;

namespace CoreProject.ViewModels
{
    public class DashboardViewModel
    {
        // Key Metrics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalBranches { get; set; }
        public int TodayCheckIns { get; set; }
        public int PendingApprovals { get; set; }

        // Computed Properties
        public int InactiveUsers => TotalUsers - ActiveUsers;
        public double AttendanceRate => TotalUsers > 0
            ? Math.Round((TodayCheckIns / (double)TotalUsers) * 100, 1)
            : 0;

        // Charts Data
        public List<MonthlyCheckIn> MonthlyCheckIns { get; set; } = new();
        public List<DepartmentAttendance> DepartmentAttendance { get; set; } = new();

        // Recent Activity Feed
        public List<RecentActivity> RecentActivities { get; set; } = new();
    }

    public class MonthlyCheckIn
    {
        public string Month { get; set; } = string.Empty;
        public int CheckIns { get; set; }
    }

    public class DepartmentAttendance
    {
        public string Department { get; set; } = string.Empty;
        public int Present { get; set; }
        public int Total { get; set; }
        public int Percentage { get; set; }
        public int Absent => Total - Present;
    }

    public class RecentActivity
    {
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "primary";

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Time;

                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";

                return Time.ToString("MMM dd");
            }
        }
    }
}
