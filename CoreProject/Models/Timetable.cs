using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class Timetable
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string? WorkingDayStartingHourMinimum { get; set; }
        public string? WorkingDayStartingHourMaximum { get; set; }
        public string? WorkingDayEndingHour { get; set; }
        public float? AverageWorkingHours { get; set; }
        public bool IsWorkingDayEndingHourEnable { get; set; }
        public int BranchID { get; set; }
        public Branch Branch { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public ICollection<TimetableConfiguration> Configurations { get; set; } = new List<TimetableConfiguration>();
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}

