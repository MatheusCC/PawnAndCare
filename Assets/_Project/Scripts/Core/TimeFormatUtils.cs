using UnityEngine;

namespace PawsAndCare.Core
{
    // Stateless helpers that turn game time into display strings, so HUDs just set the result on a
    // label and hold no formatting logic of their own.
    public static class TimeFormatUtils
    {
        private const float MINUTES_PER_HOUR = 60.0f;

        // Formats an hour and minute as a 24-hour clock string, e.g. (9, 5) → "09:05".
        public static string FormatClock(int hour, int minute)
        {
            return $"{hour:00}:{minute:00}";
        }

        // Formats a time of day in fractional hours (0–24) as a clock string, e.g. 9.25 → "09:15".
        public static string FormatTimeOfDay(float hours)
        {
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * MINUTES_PER_HOUR);

            return FormatClock(hour, minute);
        }
    }
}
