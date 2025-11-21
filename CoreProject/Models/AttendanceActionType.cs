using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class AttendanceActionType
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? DisplayName_En { get; set; }
        public string? DisplayName_Ar { get; set; }
        public int? BranchID { get; set; }
        public Branch? Branch { get; set; }

        public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
    }
}
