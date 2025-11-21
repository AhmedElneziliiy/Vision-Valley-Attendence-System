using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class Organization
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;

        public string? LogoUrl { get; set; }  // Full image URL

        public bool PassThrough { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }
}
