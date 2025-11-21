using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{

    public class OrganizationMode
    {
        public int ID { get; set; }

        // Foreign Key to Organization
        public int OrganizationID { get; set; }
        public Organization Organization { get; set; }

        public bool IsMocked { get; set; }  // Mock data for testing
        public string VersionNumber { get; set; }
        public string DeviceID { get; set; }
    }
}
