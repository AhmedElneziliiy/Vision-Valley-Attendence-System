using System;
using CoreProject.Models;
using CoreProject.Services;

namespace CoreProject.Helpers
{
    public static class TimezoneHelper
    {
        /// <summary>
        /// Converts a DateTime stored in the database (UTC) to the branch's local timezone
        /// </summary>
        public static DateTime ToLocalTime(this DateTime utcDateTime, int branchTimezone)
        {
            var timezoneService = new TimezoneService();
            return timezoneService.ConvertUtcToLocal(utcDateTime, branchTimezone);
        }

        /// <summary>
        /// Formats a DateTime for display in the branch's local timezone
        /// </summary>
        public static string ToLocalTimeString(this DateTime utcDateTime, int branchTimezone, string format = "yyyy-MM-dd HH:mm")
        {
            var timezoneService = new TimezoneService();
            var localTime = timezoneService.ConvertUtcToLocal(utcDateTime, branchTimezone);
            return localTime.ToString(format);
        }

        /// <summary>
        /// Gets the timezone name for display
        /// </summary>
        public static string GetTimezoneName(int timezoneValue)
        {
            return timezoneValue switch
            {
                0 => "UTC",
                1 => "Asia/Dubai (UAE)",
                2 => "Africa/Cairo (Egypt)",
                3 => "Europe/London (UK)",
                4 => "America/New_York (EST)",
                _ => "UTC"
            };
        }

        /// <summary>
        /// Gets the timezone offset as a string (e.g., "+02:00")
        /// </summary>
        public static string GetTimezoneOffset(int timezoneValue)
        {
            var timezoneService = new TimezoneService();
            var timeZoneInfo = timezoneService.GetTimeZoneInfo(timezoneValue);
            var offset = timeZoneInfo.GetUtcOffset(DateTime.UtcNow);
            return offset.ToString(@"hh\:mm");
        }
    }
}
