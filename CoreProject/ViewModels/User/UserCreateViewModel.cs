using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.ViewModels
{
    public class UserCreateViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        public string Password { get; set; } = null!;

        [Required]
        public string DisplayName { get; set; } = null!;

        public string? Mobile { get; set; }

        public char? Gender { get; set; }

        public string? Address { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public string Role { get; set; } = null!;

        public IEnumerable<SelectListItem> Branches { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Departments { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Roles { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
