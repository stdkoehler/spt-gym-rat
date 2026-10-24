using System;

namespace FlexiGym.Client;

public static class TimeFormatUtil
{
    /// <summary>
    /// Formats remaining seconds into a user-friendly string like "14 hours 25 minutes left".
    /// </summary>
    public static string FormatTimeLeft(int totalSeconds)
    {
        if (totalSeconds <= 0) return string.Empty;

        var span = TimeSpan.FromSeconds(totalSeconds);
        int hours = (int)span.TotalHours;
        int minutes = span.Minutes;
        int seconds = span.Seconds;

        if (hours > 0 && minutes > 0)
        {
            string h = hours == 1 ? "1 hour" : $"{hours} hours";
            string m = minutes == 1 ? "1 minute" : $"{minutes} minutes";
            return $"{h} {m} left";
        }

        if (hours > 0)
        {
            string h = hours == 1 ? "1 hour" : $"{hours} hours";
            return $"{h} left";
        }

        if (minutes > 0)
        {
            string m = minutes == 1 ? "1 minute" : $"{minutes} minutes";
            return $"{m} left";
        }

        string s = seconds == 1 ? "1 second" : $"{Math.Max(1, seconds)} seconds";
        return $"{s} left";
    }
}
