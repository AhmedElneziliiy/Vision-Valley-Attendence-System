using System;
using System.Collections.Generic;

namespace CoreProject.ViewModels
{
    public class TimetableDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public string? WorkingDayStartingHourMinimum { get; set; }
        public string? WorkingDayStartingHourMaximum { get; set; }
        public string? WorkingDayEndingHour { get; set; }
        public float? AverageWorkingHours { get; set; }
        public bool IsWorkingDayEndingHourEnable { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }

        // Related configurations
        public List<TimetableConfigurationDetailViewModel> Configurations { get; set; } = new();

        // Users assigned to this timetable
        public List<TimetableUserViewModel> AssignedUsers { get; set; } = new();

        // Computed Properties
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";
        public string WorkingHoursDisplay => AverageWorkingHours.HasValue
            ? $"{AverageWorkingHours.Value:F1} hours"
            : "Not set";
    }

    public class TimetableConfigurationDetailViewModel
    {
        public int Id { get; set; }
        public string ConfigurationName { get; set; } = null!;
        public string? Value { get; set; }
    }

    public class TimetableUserViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Email { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
    }
}
