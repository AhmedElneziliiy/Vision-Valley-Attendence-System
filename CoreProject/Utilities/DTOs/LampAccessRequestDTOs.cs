using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreProject.Utilities.DTOs
{
    // ===== REQUEST DTOs =====

    public class LampAccessRequestDto
    {
        [Required(ErrorMessage = "LampID is required")]
        public int LampID { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class LampAccessResponseRequestDto
    {
        [Required(ErrorMessage = "RequestID is required")]
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Action is required")]
        [RegularExpression("^(Approve|Decline)$", ErrorMessage = "Action must be 'Approve' or 'Decline'")]
        public string Action { get; set; } = null!;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    // ===== RESPONSE DTOs =====

    public class LampAccessResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public LampAccessRequestDetailsDto? Request { get; set; }
    }

    public class LampAccessRequestDetailsDto
    {
        public int ID { get; set; }
        public int LampID { get; set; }
        public string LampName { get; set; } = null!;
        public string LampDeviceID { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime RequestedAt { get; set; }
        public DateTime TimeoutAt { get; set; }
        public string? Reason { get; set; }
        public DateTime? ApprovedUntil { get; set; }
        public string? RespondedBy { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? ResponseNotes { get; set; }
        public bool IsAutoClosed { get; set; }
        public DateTime? AutoClosedAt { get; set; }
    }

    public class LampAccessRequestListDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public List<LampAccessRequestDetailsDto> Requests { get; set; } = new List<LampAccessRequestDetailsDto>();
    }
}
