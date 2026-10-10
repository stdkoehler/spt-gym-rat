namespace FlexiGym.Tests;

/// <summary>
/// Guards the contract between config.jsonc and Config.cs: the shipped file must parse
/// (comments and all), must need no corrections, and out-of-range values must be clamped.
/// </summary>
public static class ConfigCheck
{
    public static (int pass, int fail) Run(string configPath)
    {
        int pass = 0, fail = 0;

        void Expect(string what, bool ok)
        {
            if (ok) pass++;
            else { fail++; Console.WriteLine($"  FAIL {what}"); }
        }

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"  FAIL missing {configPath}");
            return (0, 1);
        }

        var cfg = Config.Load(configPath);
        var defaults = new Config();

        Expect("shipped config.jsonc needs no sanitising", cfg.Sanitise().Count == 0);
        Expect("shipped enabled matches the code default", cfg.Enabled == defaults.Enabled);
        Expect("shipped gymResetMinutes matches the code default", cfg.GymResetMinutes == defaults.GymResetMinutes);

        var high = new Config { GymResetMinutes = 999999 };
        var notes = high.Sanitise();
        Expect("too-large gymResetMinutes is clamped", high.GymResetMinutes == Config.MaxResetMinutes && notes.Count == 1);

        var low = new Config { GymResetMinutes = 0 };
        low.Sanitise();
        Expect("zero gymResetMinutes is clamped", low.GymResetMinutes == Config.MinResetMinutes);

        Console.WriteLine($"  {pass} passed, {fail} failed");
        return (pass, fail);
    }
}
