using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreProject.ViewModels
{
    public class TimetableEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Timetable name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Branch is required")]
        public int BranchId { get; set; }

        [Display(Name = "Working Day Starting Hour (Minimum)")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Please enter a valid time in HH:mm format")]
        public string? WorkingDayStartingHourMinimum { get; set; }

        [Display(Name = "Working Day Starting Hour (Maximum)")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Please enter a valid time in HH:mm format")]
        public string? WorkingDayStartingHourMaximum { get; set; }

        [Display(Name = "Working Day Ending Hour")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Please enter a valid time in HH:mm format")]
        public string? WorkingDayEndingHour { get; set; }

        [Display(Name = "Average Working Hours")]
        [Range(0, 24, ErrorMessage = "Working hours must be between 0 and 24")]
        public float? AverageWorkingHours { get; set; }

        [Display(Name = "Enable Working Day Ending Hour")]
        public bool IsWorkingDayEndingHourEnable { get; set; }

        public bool IsActive { get; set; }

        // Dropdown lists
        public IEnumerable<SelectListItem> Branches { get; set; } = Enumerable.Empty<SelectListItem>();

        // Related configurations
        public List<TimetableConfigurationViewModel> Configurations { get; set; } = new();
    }

    public class TimetableConfigurationViewModel
    {
        public int Id { get; set; }
        public int ConfigurationId { get; set; }
        public string ConfigurationName { get; set; } = null!;
        public string? Value { get; set; }
    }
}
