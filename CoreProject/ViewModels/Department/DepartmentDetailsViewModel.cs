using System;
using System.Collections.Generic;

namespace CoreProject.ViewModels
{
    public class DepartmentDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string BranchName { get; set; } = null!;
        public int BranchId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Statistics
        public int UserCount { get; set; }
        public int ActiveUserCount { get; set; }

        // Related Users
        public List<UserInfo> Users { get; set; } = new();

        // Computed Properties
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Mobile { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
