using System;

namespace CoreProject.ViewModels
{
    public class BranchViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int TimeZone { get; set; }
        public string? Weekend { get; set; }
        public bool IsMainBranch { get; set; }
        public bool IsActive { get; set; }
        public int DepartmentCount { get; set; }
        public int TimetableCount { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }

        // Computed Properties
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";
        public string MainBranchBadge => IsMainBranch ? "Main Branch" : "Branch";
        public string MainBranchClass => IsMainBranch ? "badge-primary" : "badge-secondary";
    }
}
