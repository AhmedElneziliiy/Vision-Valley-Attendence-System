using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreProject.Models
{
    public class Device
    {
        public int ID { get; set; }

        /// <summary>
        /// Unique Device Identifier (e.g., "100000000")
        /// Used for mobile app attendance tracking
        /// </summary>
        [MaxLength(100)]
        public string? DeviceID { get; set; }

        public char? DeviceType { get; set; }
        public int? CoverageArea { get; set; }
        public int BranchID { get; set; }
        public Branch Branch { get; set; } = null!;
        public bool? IsSignedIn { get; set; }
        public bool? IsSignedOut { get; set; }
        public bool IsPassThrough { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Device access control URL identifier (max 450 characters for indexing)
        /// Used by hardware devices to query access control state
        /// </summary>
        [MaxLength(450)]
        public string? AccessControlURL { get; set; }

        /// <summary>
        /// Access control state (0 or 1) - automatically reset to 0 after reading
        /// </summary>
        public int? AccessControlState { get; set; }
    }

}
