using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CoreProject.Models
{
    public class Branch
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public int OrganizationID { get; set; }
        public Organization Organization { get; set; } = null!;
        public int TimeZone { get; set; }
        public string? Weekend { get; set; }
        public string? NationalHolidays { get; set; }
        public bool IsMainBranch { get; set; } = false;
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Enable/disable face verification for all users in this branch
        /// </summary>
        public bool IsFaceVerificationEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<Device> Devices { get; set; } = new List<Device>();
        public ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
        public ICollection<AttendanceActionType> ActionTypes { get; set; } = new List<AttendanceActionType>();
    }

}
