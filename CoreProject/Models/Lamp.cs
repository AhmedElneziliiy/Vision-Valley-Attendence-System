using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CoreProject.Models
{
    /// <summary>
    /// Lamp entity for ESP32-based access control lamps
    /// Controls physical lamps at branch doors based on timetable schedules
    /// </summary>
    public class Lamp
    {
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// ESP32 Device ID (must match device_id in ESP32 code)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string DeviceID { get; set; } = null!;

        /// <summary>
        /// Friendly name for the lamp (e.g., "Cairo Branch Main Door Lamp")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Optional description
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        // ===== Relationships =====

        /// <summary>
        /// Branch this lamp belongs to
        /// </summary>
        [Required]
        [ForeignKey(nameof(Branch))]
        public int BranchID { get; set; }
        public virtual Branch Branch { get; set; } = null!;

        /// <summary>
        /// Timetable that controls this lamp's schedule
        /// </summary>
        [Required]
        [ForeignKey(nameof(Timetable))]
        public int TimetableID { get; set; }
        public virtual Timetable Timetable { get; set; } = null!;

        // ===== State Tracking =====

        /// <summary>
        /// Current lamp state: 0 = OFF, 1 = ON
        /// </summary>
        [Required]
        public int CurrentState { get; set; } = 0;

        /// <summary>
        /// Timestamp of last state change
        /// </summary>
        public DateTime? LastStateChange { get; set; }

        /// <summary>
        /// Manual override enabled (admin can turn ON/OFF regardless of schedule)
        /// </summary>
        [Required]
        public bool ManualOverride { get; set; } = false;

        /// <summary>
        /// Manual override state (only applies if ManualOverride is true)
        /// </summary>
        public int? ManualOverrideState { get; set; }

        // ===== WebSocket Connection Info =====

        /// <summary>
        /// Is the ESP32 currently connected via WebSocket?
        /// </summary>
        [Required]
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// Timestamp of last successful connection
        /// </summary>
        public DateTime? LastConnectionTime { get; set; }

        /// <summary>
        /// Timestamp of last disconnection
        /// </summary>
        public DateTime? LastDisconnectionTime { get; set; }

        /// <summary>
        /// WebSocket connection ID (SignalR/WebSocket internal ID)
        /// </summary>
        [StringLength(100)]
        public string? ConnectionID { get; set; }

        // ===== Management =====

        /// <summary>
        /// Is this lamp active/enabled?
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Created timestamp
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Helper method to determine if lamp should be ON based on timetable
        /// Includes grace period: 1 hour before start time and 1 hour after end time
        /// </summary>
        public bool ShouldBeOn(DateTime branchLocalTime, int graceHoursBefore = 1, int graceHoursAfter = 1, CoreProject.Context.ApplicationDbContext? context = null)
        {
            // NEW: Check for active approved access requests (highest priority)
            if (context != null)
            {
                var activeRequest = context.LampAccessRequests
                    .AsNoTracking()
                    .FirstOrDefault(r => r.LampID == this.ID
                                      && r.Status == "Approved"
                                      && !r.IsAutoClosed
                                      && r.ApprovedUntil > DateTime.UtcNow);

                if (activeRequest != null)
                {
                    return true; // Keep lamp ON due to active access request
                }
            }

            // If manual override is enabled, use that state
            if (ManualOverride && ManualOverrideState.HasValue)
            {
                return ManualOverrideState.Value == 1;
            }

            // If not active, should be OFF
            if (!IsActive)
            {
                return false;
            }

            // Check if timetable exists
            if (Timetable == null)
            {
                return false;
            }

            // Parse timetable hours
            if (string.IsNullOrEmpty(Timetable.WorkingDayStartingHourMinimum) ||
                string.IsNullOrEmpty(Timetable.WorkingDayEndingHour))
            {
                return false;
            }

            if (!TimeSpan.TryParse(Timetable.WorkingDayStartingHourMinimum, out var startTime) ||
                !TimeSpan.TryParse(Timetable.WorkingDayEndingHour, out var endTime))
            {
                return false;
            }

            var currentTime = branchLocalTime.TimeOfDay;

            // Apply grace periods
            var effectiveStartTime = startTime.Subtract(TimeSpan.FromHours(graceHoursBefore));
            var effectiveEndTime = endTime.Add(TimeSpan.FromHours(graceHoursAfter));

            // Handle edge case where grace period extends past midnight
            if (effectiveStartTime < TimeSpan.Zero)
            {
                effectiveStartTime = TimeSpan.Zero;
            }
            if (effectiveEndTime > TimeSpan.FromHours(24))
            {
                effectiveEndTime = TimeSpan.FromHours(24);
            }

            // Check if current time is within working hours + grace periods
            return currentTime >= effectiveStartTime && currentTime <= effectiveEndTime;
        }
    }
}
