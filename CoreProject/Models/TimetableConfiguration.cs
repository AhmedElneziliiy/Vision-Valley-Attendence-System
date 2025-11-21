using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{ 
    public class TimetableConfiguration
    {
        public int ID { get; set; }
        public int ConfigurationID { get; set; }
        public Configuration Configuration { get; set; } = null!;
        public int TimetableID { get; set; }
        public Timetable Timetable { get; set; } = null!;
        public string? Value { get; set; }
    }
}
