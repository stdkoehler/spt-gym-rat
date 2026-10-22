using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlexiGym;

/// <summary>
/// Player-facing settings. Mirrors config.jsonc; keep the two in sync and keep every
/// default here identical to the value shipped in the file.
///
/// This file deliberately has no SPT dependency so tests/FlexiGym.Tests can compile it directly.
/// </summary>
public class Config
{
    public const int MinResetMinutes = 1;
    public const int MaxResetMinutes = 10080; // one week

    public bool Enabled { get; set; } = true;

    /// <summary>Minutes until gym muscle pain (and with it the gym "reset") wears off. Vanilla is 1440 (24h).</summary>
    public int GymResetMinutes { get; set; } = 1440;

    /// <summary>Consuming one of these items (template ids) in the hideout resets the gym immediately.</summary>
    public List<string> ResetItems { get; set; } = ["5e8f3423fd7471236e6e3b64"]; // Kvass

    /// <summary>Only items found in raid trigger the reset.</summary>
    public bool RequireFoundInRaid { get; set; } = true;

    /// <summary>
    /// Parsed by hand rather than through ModHelper, because config.jsonc carries
    /// comments and trailing commas that a strict reader rejects.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static Config Load(string path)
    {
        return JsonSerializer.Deserialize<Config>(File.ReadAllText(path), Options) ?? new Config();
    }

    /// <summary>
    /// Clamp every value to a meaningful range. Returns a human-readable note per change
    /// so the mod can log what it corrected instead of failing on a typo.
    /// </summary>
    public List<string> Sanitise()
    {
        var notes = new List<string>();

        var clamped = Math.Clamp(GymResetMinutes, MinResetMinutes, MaxResetMinutes);
        if (clamped != GymResetMinutes)
        {
            notes.Add($"{nameof(GymResetMinutes)} was {GymResetMinutes}, clamped to {clamped}");
            GymResetMinutes = clamped;
        }

        return notes;
    }
}
