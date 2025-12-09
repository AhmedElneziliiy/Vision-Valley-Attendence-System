using System;
using CoreProject.Models;

namespace CoreProject.Services
{
    public interface ITimezoneService
    {
        /// <summary>
        /// Converts a timezone integer value to IANA timezone string
        /// </summary>
        string GetTimezoneString(int timezoneValue);

        /// <summary>
        /// Converts UTC DateTime to branch's local timezone
        /// </summary>
        DateTime ConvertUtcToLocal(DateTime utcDateTime, int timezoneValue);

        /// <summary>
        /// Converts branch's local DateTime to UTC
        /// </summary>
        DateTime ConvertLocalToUtc(DateTime localDateTime, int timezoneValue);

        /// <summary>
        /// Gets the current local time for a branch
        /// </summary>
        DateTime GetBranchNow(int timezoneValue);

        /// <summary>
        /// Gets the current local date for a branch
        /// </summary>
        DateTime GetBranchToday(int timezoneValue);

        /// <summary>
        /// Converts TimeSpan (UTC) to local TimeSpan for display
        /// </summary>
        TimeSpan ConvertUtcTimeToLocal(TimeSpan utcTime, DateTime date, int timezoneValue);

        /// <summary>
        /// Gets TimeZoneInfo from timezone value
        /// </summary>
        TimeZoneInfo GetTimeZoneInfo(int timezoneValue);
    }

    public class TimezoneService : ITimezoneService
    {
        /// <summary>
        /// Converts timezone integer (0-5) to IANA timezone string
        /// 0 = UTC, 1 = Asia/Dubai, 2 = Africa/Cairo, 3 = Europe/London, 4 = America/New_York, 5 = Asia/Riyadh
        /// </summary>
        public string GetTimezoneString(int timezoneValue)
        {
            return timezoneValue switch
            {
                0 => "UTC",
                1 => "Arabian Standard Time", // Asia/Dubai (UAE)
                2 => "Egypt Standard Time",    // Africa/Cairo (Egypt)
                3 => "GMT Standard Time",      // Europe/London (UK)
                4 => "Eastern Standard Time",  // America/New_York (EST)
                5 => "Arab Standard Time",     // Asia/Riyadh (Saudi Arabia - UTC+3)
                _ => "UTC"
            };
        }

        /// <summary>
        /// Gets TimeZoneInfo object for the given timezone value
        /// </summary>
        public TimeZoneInfo GetTimeZoneInfo(int timezoneValue)
        {
            var timezoneId = GetTimezoneString(timezoneValue);
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }

        /// <summary>
        /// Converts UTC DateTime to the branch's local timezone
        /// </summary>
        public DateTime ConvertUtcToLocal(DateTime utcDateTime, int timezoneValue)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            var targetTimeZone = GetTimeZoneInfo(timezoneValue);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, targetTimeZone);
        }

        /// <summary>
        /// Converts branch's local DateTime to UTC
        /// </summary>
        public DateTime ConvertLocalToUtc(DateTime localDateTime, int timezoneValue)
        {
            var sourceTimeZone = GetTimeZoneInfo(timezoneValue);
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, sourceTimeZone);
        }

        /// <summary>
        /// Gets the current date and time in the branch's local timezone
        /// </summary>
        public DateTime GetBranchNow(int timezoneValue)
        {
            var utcNow = DateTime.UtcNow;
            return ConvertUtcToLocal(utcNow, timezoneValue);
        }

        /// <summary>
        /// Gets the current date (start of day) in the branch's local timezone
        /// </summary>
        public DateTime GetBranchToday(int timezoneValue)
        {
            var branchNow = GetBranchNow(timezoneValue);
            return branchNow.Date;
        }

        /// <summary>
        /// Converts a UTC TimeSpan to local TimeSpan for a specific date
        /// This is useful for attendance records where time is stored as TimeSpan
        /// </summary>
        public TimeSpan ConvertUtcTimeToLocal(TimeSpan utcTime, DateTime date, int timezoneValue)
        {
            // Create a UTC DateTime from the date and time
            var utcDateTime = date.Date.Add(utcTime);
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            // Convert to local time
            var localDateTime = ConvertUtcToLocal(utcDateTime, timezoneValue);

            // Return the TimeOfDay portion
            return localDateTime.TimeOfDay;
        }
    }
}
