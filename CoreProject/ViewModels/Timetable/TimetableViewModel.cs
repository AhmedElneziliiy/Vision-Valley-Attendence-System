using System;

namespace CoreProject.ViewModels
{
    public class TimetableViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string BranchName { get; set; } = null!;
        public int BranchId { get; set; }
        public string? WorkingDayStartingHourMinimum { get; set; }
        public string? WorkingDayStartingHourMaximum { get; set; }
        public string? WorkingDayEndingHour { get; set; }
        public float? AverageWorkingHours { get; set; }
        public bool IsWorkingDayEndingHourEnable { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public int ConfigurationCount { get; set; }

        // Computed Properties
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";
        public string WorkingHoursDisplay => AverageWorkingHours.HasValue
            ? $"{AverageWorkingHours.Value:F1} hrs"
            : "Not set";
    }
}
