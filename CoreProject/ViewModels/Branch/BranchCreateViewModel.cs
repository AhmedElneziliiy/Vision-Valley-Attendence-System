using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreProject.ViewModels
{
    public class BranchCreateViewModel
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public int OrganizationId { get; set; }

        public int TimeZone { get; set; }

        public string? Weekend { get; set; }

        public string? NationalHolidays { get; set; }

        public bool IsMainBranch { get; set; }

        public bool IsActive { get; set; } = true;

        // Timetable information (created automatically with branch)
        [Required]
        public string TimetableName { get; set; } = null!;

        public string? WorkingDayStartingHourMinimum { get; set; }

        public string? WorkingDayStartingHourMaximum { get; set; }

        public string? WorkingDayEndingHour { get; set; }

        public float? AverageWorkingHours { get; set; }

        public bool IsWorkingDayEndingHourEnable { get; set; }

        // Departments to create with branch
        public List<string> DepartmentNames { get; set; } = new();

        // Dropdown lists
        public IEnumerable<SelectListItem> Organizations { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> TimeZones { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
