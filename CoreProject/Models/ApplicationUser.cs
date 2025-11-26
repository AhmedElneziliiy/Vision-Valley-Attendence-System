using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace CoreProject.Models
{

    public class ApplicationUser : IdentityUser<int>
    {
        public string DisplayName { get; set; } = null!;
        public string? Mobile { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? VacationBalance { get; set; } = 21;
        public string? Address { get; set; }
        public char? Gender { get; set; }

        /// <summary>
        /// Unique Device Identifier for mobile app authentication
        /// Used to bind a user to a specific mobile device
        /// </summary>
        public string? UDID { get; set; }

        /// <summary>
        /// Enable/disable face verification for this specific user
        /// Both branch and user flags must be true for face verification to be required
        /// </summary>
        public bool IsFaceVerificationEnabled { get; set; } = false;

        /// <summary>
        /// Face embedding (512 floats = 2048 bytes) for face recognition
        /// Stored as byte array for efficient storage
        /// </summary>
        public byte[]? FaceEmbedding { get; set; }

        /// <summary>
        /// Timestamp when face was enrolled for this user
        /// </summary>
        public DateTime? FaceEnrolledAt { get; set; }

        // Hierarchy
        public int? ManagerID { get; set; }
        public ApplicationUser? Manager { get; set; }
        public ICollection<ApplicationUser> Subordinates { get; set; } = new List<ApplicationUser>();

        // FKs
        public int DepartmentID { get; set; }
        public Department Department { get; set; } = null!;
        public int BranchID { get; set; }
        public Branch Branch { get; set; } = null!;
        public int? TimetableID { get; set; }
        public Timetable? Timetable { get; set; }

        // Navigation
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public UserImage? Image { get; set; }
    }
}
