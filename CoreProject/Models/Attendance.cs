using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class Attendance
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public DateTime Date { get; set; }
        public string? FirstCheckIn { get; set; }   // e.g., "09:15" (local branch time)
        public string? LastCheckOut { get; set; }   // e.g., "17:30" (local branch time)
        public int Duration { get; set; }           // minutes

        // Attendance Status Fields
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
        public int? MinutesLate { get; set; }       // Positive if late, negative if early, null if on time or absent

        public bool HRPosted { get; set; } = false;
        public int? HRUserID { get; set; }
        public ApplicationUser? HRUser { get; set; }
        public DateTime? HRPostedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }
}
