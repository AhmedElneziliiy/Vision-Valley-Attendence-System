using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class Configuration
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<TimetableConfiguration> TimetableConfigurations { get; set; } = new List<TimetableConfiguration>();
    }
}

