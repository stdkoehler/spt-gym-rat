using GymRat.Client;

namespace GymRat.Tests;

public static class TimeFormatCheck
{
    public static (int pass, int fail) Run()
    {
        int pass = 0, fail = 0;

        void Check(string label, string expected, string actual)
        {
            if (expected == actual)
            {
                pass++;
            }
            else
            {
                fail++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  FAIL: {label} — expected '{expected}', got '{actual}'");
                Console.ResetColor();
            }
        }

        // Zero & negative
        Check("Zero seconds", "", TimeFormatUtil.FormatTimeLeft(0));
        Check("Negative seconds", "", TimeFormatUtil.FormatTimeLeft(-50));

        // Seconds only
        Check("1 second", "1 second left", TimeFormatUtil.FormatTimeLeft(1));
        Check("45 seconds", "45 seconds left", TimeFormatUtil.FormatTimeLeft(45));

        // Minutes only
        Check("1 minute (60s)", "1 minute left", TimeFormatUtil.FormatTimeLeft(60));
        Check("2 minutes (120s)", "2 minutes left", TimeFormatUtil.FormatTimeLeft(120));
        Check("59 minutes (3540s)", "59 minutes left", TimeFormatUtil.FormatTimeLeft(3540));
        Check("59 minutes with trailing seconds (3599s)", "59 minutes left", TimeFormatUtil.FormatTimeLeft(3599));

        // Exact hours
        Check("1 hour exact (3600s)", "1 hour left", TimeFormatUtil.FormatTimeLeft(3600));
        Check("2 hours exact (7200s)", "2 hours left", TimeFormatUtil.FormatTimeLeft(7200));
        Check("24 hours exact (86400s)", "24 hours left", TimeFormatUtil.FormatTimeLeft(86400));
        Check("48 hours exact (172800s)", "48 hours left", TimeFormatUtil.FormatTimeLeft(172800));

        // Hours and minutes
        Check("1 hour 1 minute (3660s)", "1 hour 1 minute left", TimeFormatUtil.FormatTimeLeft(3660));
        Check("1 hour 2 minutes (3720s)", "1 hour 2 minutes left", TimeFormatUtil.FormatTimeLeft(3720));
        Check("14 hours 22 minutes (51720s)", "14 hours 22 minutes left", TimeFormatUtil.FormatTimeLeft(51720));
        Check("23 hours 59 minutes (86340s)", "23 hours 59 minutes left", TimeFormatUtil.FormatTimeLeft(86340));
        Check("38 hours 10 minutes (137400s)", "38 hours 10 minutes left", TimeFormatUtil.FormatTimeLeft(137400));

        Console.WriteLine($"  {pass} passed, {fail} failed");
        return (pass, fail);
    }
}
