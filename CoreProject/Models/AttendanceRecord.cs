using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class AttendanceRecord
    {
        public int ID { get; set; }
        public int AttendanceID { get; set; }
        public Attendance Attendance { get; set; } = null!;
        public TimeSpan Time { get; set; }           // e.g., 09:15:00
        public bool IsCheckIn { get; set; }          // true = in, false = out
        public bool? IsAutomated { get; set; }       // true = device, false = manual
        public int? FaceValidation { get; set; }     // 1 = success, 0 = fail
        public int? ReasonID { get; set; }
        public AttendanceActionType? Reason { get; set; }
    }
}
