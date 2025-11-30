using System;
using System.ComponentModel.DataAnnotations;

namespace CoreProject.Models
{
    public class LampAccessRequest
    {
        [Key]
        public int ID { get; set; }

        // Request Info
        [Required]
        public int LampID { get; set; }
        public virtual Lamp Lamp { get; set; } = null!;

        [Required]
        public int UserID { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Reason { get; set; }

        // Status: Pending, Approved, Declined, Timeout
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        // Response Info
        public int? RespondedByUserID { get; set; }
        public virtual ApplicationUser? RespondedByUser { get; set; }

        public DateTime? RespondedAt { get; set; }

        [MaxLength(500)]
        public string? ResponseNotes { get; set; }

        // Timeout Tracking
        [Required]
        public DateTime TimeoutAt { get; set; } // RequestedAt + 5 minutes

        // Auto-Close Tracking (for approved requests)
        public DateTime? ApprovedUntil { get; set; } // RespondedAt + 1 hour

        [Required]
        public bool IsAutoClosed { get; set; } = false;

        public DateTime? AutoClosedAt { get; set; }

        // Audit
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
