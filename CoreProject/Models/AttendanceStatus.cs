namespace CoreProject.Models
{
    /// <summary>
    /// Defines the status of an attendance record based on check-in time
    /// </summary>
    public enum AttendanceStatus
    {
        /// <summary>
        /// User has not checked in yet
        /// </summary>
        Absent = 0,

        /// <summary>
        /// User checked in within the acceptable time window
        /// </summary>
        OnTime = 1,

        /// <summary>
        /// User checked in after the maximum start time but within grace period (1-15 minutes late)
        /// </summary>
        Late = 2,

        /// <summary>
        /// User checked in significantly after the maximum start time (more than 15 minutes late)
        /// </summary>
        VeryLate = 3,

        /// <summary>
        /// User checked in before the minimum start time
        /// </summary>
        Early = 4
    }
}
