using System;
using System.Collections.Generic;

namespace CoreProject.ViewModels
{
    public class BranchDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string OrganizationName { get; set; } = null!;
        public int TimeZone { get; set; }
        public string? Weekend { get; set; }
        public string? NationalHolidays { get; set; }
        public bool IsMainBranch { get; set; }
        public bool IsActive { get; set; }
        public bool IsFaceVerificationEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Statistics
        public int DepartmentCount { get; set; }
        public int TimetableCount { get; set; }
        public int UserCount { get; set; }
        public int ActiveUserCount { get; set; }

        // Related Data
        public List<DepartmentInfo> Departments { get; set; } = new();
        public List<TimetableInfo> Timetables { get; set; } = new();
        public List<StaffInfo> HRStaff { get; set; } = new();
        public List<StaffInfo> Managers { get; set; } = new();

        // Computed Properties
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";
        public string MainBranchBadge => IsMainBranch ? "Main Branch" : "Branch";
        public string MainBranchClass => IsMainBranch ? "badge-primary" : "badge-secondary";
    }

    public class DepartmentInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
    }

    public class TimetableInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public float? AverageWorkingHours { get; set; }
    }

    public class StaffInfo
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Mobile { get; set; }
        public bool IsActive { get; set; }
        public string? DepartmentName { get; set; }
    }
}
