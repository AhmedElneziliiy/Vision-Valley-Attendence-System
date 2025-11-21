using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreProject.ViewModels
{
    public class UserEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string DisplayName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        public string? Mobile { get; set; }

        public string? Address { get; set; }

        public char? Gender { get; set; }

        [Range(0, 365)]
        public int? VacationBalance { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        public int? TimetableId { get; set; }

        public int? ManagerId { get; set; }

        [Required]
        public string Role { get; set; } = null!;

        public bool IsActive { get; set; }

        // Password fields - optional for edit
        [MinLength(6)]
        public string? NewPassword { get; set; }

        // Profile photo upload
        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhoto { get; set; }

        // Current photo URL for display
        public string? CurrentPhotoUrl { get; set; }

        // Dropdown lists
        public IEnumerable<SelectListItem> Branches { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Departments { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Timetables { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Managers { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Roles { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Genders { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "M", Text = "Male" },
            new SelectListItem { Value = "F", Text = "Female" }
        };
    }
}
