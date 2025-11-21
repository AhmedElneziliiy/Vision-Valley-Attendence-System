using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreProject.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Mobile { get; set; }
        public char? Gender { get; set; }
        public string? Address { get; set; }
        public string BranchName { get; set; } = null!;
        public string DepartmentName { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? VacationBalance { get; set; }

        // Computed Properties
        public string RoleBadgeClass => GetRoleBadgeClass();
        public string PrimaryRole => Roles.FirstOrDefault() ?? "No Role";
        public string StatusBadge => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";

        private string GetRoleBadgeClass()
        {
            var role = Roles.FirstOrDefault()?.ToLower();
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