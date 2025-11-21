using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreProject.ViewModels
{
    public class DeviceEditViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Device ID")]
        [StringLength(100, ErrorMessage = "Device ID cannot exceed 100 characters")]
        public string? DeviceID { get; set; }

        [Required(ErrorMessage = "Device type is required")]
        [Display(Name = "Device Type")]
        public string? DeviceType { get; set; }

        [Display(Name = "Coverage Area")]
        [Range(0, 10000, ErrorMessage = "Coverage area must be between 0 and 10000")]
        public int? CoverageArea { get; set; }

        [Required(ErrorMessage = "Branch is required")]
        [Display(Name = "Branch")]
        public int BranchId { get; set; }

        [Display(Name = "Enable Sign In")]
        public bool IsSignedIn { get; set; }

        [Display(Name = "Enable Sign Out")]
        public bool IsSignedOut { get; set; }

        [Display(Name = "Pass Through Mode")]
        public bool IsPassThrough { get; set; }

        [Display(Name = "Device Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Display(Name = "Access Control URL")]
        [StringLength(500, ErrorMessage = "Access Control URL cannot exceed 500 characters")]
        public string? AccessControlURL { get; set; }

        [Display(Name = "Access Control State")]
        public int? AccessControlState { get; set; }

        // Dropdown lists
        public IEnumerable<SelectListItem> Branches { get; set; } = Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> DeviceTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "I", Text = "In Device (Entry)" },
            new SelectListItem { Value = "O", Text = "Out Device (Exit)" },
            new SelectListItem { Value = "B", Text = "Both (Entry/Exit)" }
        };

        public IEnumerable<SelectListItem> AccessControlStates { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Active" },
            new SelectListItem { Value = "0", Text = "Inactive" }
        };
    }
}
