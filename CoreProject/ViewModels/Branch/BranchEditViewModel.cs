using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreProject.ViewModels
{
    public class BranchEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public int OrganizationId { get; set; }

        public int TimeZone { get; set; }

        public string? Weekend { get; set; }

        public string? NationalHolidays { get; set; }

        public bool IsMainBranch { get; set; }

        public bool IsActive { get; set; }

        // Dropdown lists
        public IEnumerable<SelectListItem> Organizations { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> TimeZones { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
