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

        Expect("shipped resetItems matches the code default", cfg.ResetItems.SequenceEqual(defaults.ResetItems));
        Expect("shipped requireFoundInRaid matches the code default", cfg.RequireFoundInRaid == defaults.RequireFoundInRaid);
        Expect("shipped activeRecoveryEnabled matches the code default", cfg.ActiveRecoveryEnabled == defaults.ActiveRecoveryEnabled);
        Expect("shipped bossKillResets matches the code default", cfg.BossKillResets == defaults.BossKillResets);
        Expect("shipped minPmcKillsForReset matches the code default", cfg.MinPmcKillsForReset == defaults.MinPmcKillsForReset);
        Expect("shipped requireSurviveForActiveRecovery matches the code default", cfg.RequireSurviveForActiveRecovery == defaults.RequireSurviveForActiveRecovery);
        Expect("shipped notifyOnActiveRecovery matches the code default", cfg.NotifyOnActiveRecovery == defaults.NotifyOnActiveRecovery);
        Expect("kvass is a default reset item", defaults.ResetItems.Contains("5e8f3423fd7471236e6e3b64"));
        Expect("requireFoundInRaid defaults to true", defaults.RequireFoundInRaid);

        var high = new Config { GymResetMinutes = 999999 };
        var notes = high.Sanitise();
        Expect("too-large gymResetMinutes is clamped", high.GymResetMinutes == Config.MaxResetMinutes && notes.Count == 1);

        var low = new Config { GymResetMinutes = 0, MinPmcKillsForReset = -5 };
        var lowNotes = low.Sanitise();
        Expect("zero gymResetMinutes is clamped", low.GymResetMinutes == Config.MinResetMinutes);
        Expect("negative minPmcKills is clamped to zero", low.MinPmcKillsForReset == 0 && lowNotes.Count == 2);

        Console.WriteLine($"  {pass} passed, {fail} failed");
        return (pass, fail);
    }
}
