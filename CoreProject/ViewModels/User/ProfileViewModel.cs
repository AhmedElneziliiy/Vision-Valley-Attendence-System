using System;
using System.ComponentModel.DataAnnotations;

namespace CoreProject.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Phone]
        [Display(Name = "Mobile Number")]
        public string? Mobile { get; set; }

        [Display(Name = "Gender")]
        public char? Gender { get; set; }

        public string? Address { get; set; }

        [Display(Name = "Branch")]
        public string BranchName { get; set; } = null!;

        [Display(Name = "Department")]
        public string? DepartmentName { get; set; }

        [Display(Name = "Role")]
        public string RoleName { get; set; } = null!;

        [Display(Name = "Vacation Balance")]
        public int? VacationBalance { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; }

        [Display(Name = "Member Since")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Profile Picture")]
        public string? ProfileImageUrl { get; set; }

        // Computed properties
        public string GenderDisplay => Gender == 'M' ? "Male" : Gender == 'F' ? "Female" : "Not Specified";
        public string StatusDisplay => IsActive ? "Active" : "Inactive";
        public string StatusClass => IsActive ? "success" : "danger";
    }

    public class UpdateProfileViewModel
    {
        [Required]
        [Display(Name = "Display Name")]
        [StringLength(100, MinimumLength = 2)]
        public string DisplayName { get; set; } = null!;

        [Phone]
        [Display(Name = "Mobile Number")]
        public string? Mobile { get; set; }

        [Display(Name = "Gender")]
        public char? Gender { get; set; }

        [Display(Name = "Address")]
        [StringLength(500)]
        public string? Address { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string NewPassword { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
