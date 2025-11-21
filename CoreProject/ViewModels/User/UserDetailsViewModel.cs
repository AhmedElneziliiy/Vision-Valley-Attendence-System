using System;
using System.Collections.Generic;

namespace CoreProject.ViewModels
{
    public class UserDetailsViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Mobile { get; set; }
        public string? Address { get; set; }
        public char? Gender { get; set; }
        public int? VacationBalance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Organization Info
        public string BranchName { get; set; } = null!;
        public string DepartmentName { get; set; } = null!;
        public string? TimetableName { get; set; }
        public List<string> Roles { get; set; } = new();

        // Manager Info
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }

        // Computed Properties
        public string GenderDisplay => Gender == 'M' ? "Male" : Gender == 'F' ? "Female" : "Not Specified";
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";
        public string PrimaryRole => Roles.Count > 0 ? Roles[0] : "No Role";
        public string RoleBadgeClass => GetRoleBadgeClass();

        private string GetRoleBadgeClass()
        {
            var role = Roles.Count > 0 ? Roles[0].ToLower() : "";
            return role switch
            {
                "admin" => "badge-admin",
                "hr" => "badge-hr",
                "manager" => "badge-manager",
                _ => "badge-employee"
            };
        }
    }
}
