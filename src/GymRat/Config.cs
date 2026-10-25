using System.Text.Json;
using System.Text.Json.Serialization;

namespace GymRat;

/// <summary>
/// Player-facing settings. Mirrors config.jsonc; keep the two in sync and keep every
/// default here identical to the value shipped in the file.
///
/// This file deliberately has no SPT dependency so tests/GymRat.Tests can compile it directly.
/// </summary>
public class Config
{
    public const int MinResetMinutes = 1;
    public const int MaxResetMinutes = 10080; // one week

    public bool Enabled { get; set; } = true;

    /// <summary>Minutes until gym muscle pain (and with it the gym "reset") wears off. Vanilla is 1440 (24h).</summary>
    public int GymResetMinutes { get; set; } = 1440;

    /// <summary>Active recovery resets gym muscle pain when completing strenuous combat feats in raid.</summary>
    public ActiveRecoveryConfig ActiveRecovery { get; set; } = new();

    /// <summary>Enable Hideout recovery crafts (Recovery Shower and Nutrition Regime).</summary>
    public CraftsConfig Crafts { get; set; } = new();

    /// <summary>Consuming food/drink items in the hideout resets the gym immediately.</summary>
    public ItemConsumeConfig ItemConsumeReset { get; set; } = new();

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

        ActiveRecovery ??= new();
        if (ActiveRecovery.MinPmcKills < 0)
        {
            notes.Add($"{nameof(ActiveRecovery)}.{nameof(ActiveRecovery.MinPmcKills)} was {ActiveRecovery.MinPmcKills}, clamped to 0");
            ActiveRecovery.MinPmcKills = 0;
        }

        Crafts ??= new();
        Crafts.Shower ??= new();
        Crafts.Nutrition ??= new();

        var showerCraft = Math.Clamp(Crafts.Shower.CraftMinutes, 1, MaxResetMinutes);
        if (showerCraft != Crafts.Shower.CraftMinutes)
        {
            notes.Add($"Crafts.Shower.{nameof(Crafts.Shower.CraftMinutes)} was {Crafts.Shower.CraftMinutes}, clamped to {showerCraft}");
            Crafts.Shower.CraftMinutes = showerCraft;
        }

        var showerLevel = Math.Clamp(Crafts.Shower.LavatoryLevel, 1, 3);
        if (showerLevel != Crafts.Shower.LavatoryLevel)
        {
            notes.Add($"Crafts.Shower.{nameof(Crafts.Shower.LavatoryLevel)} was {Crafts.Shower.LavatoryLevel}, clamped to {showerLevel}");
            Crafts.Shower.LavatoryLevel = showerLevel;
        }

        if (!IsValidHex24(Crafts.Shower.EndProduct))
        {
            notes.Add($"Crafts.Shower.{nameof(Crafts.Shower.EndProduct)} '{Crafts.Shower.EndProduct}' was not a valid 24-character hex ID, reset to default");
            Crafts.Shower.EndProduct = "5e2af47786f7746d404f3aaa";
        }

        var nutritionCraft = Math.Clamp(Crafts.Nutrition.CraftMinutes, 1, MaxResetMinutes);
        if (nutritionCraft != Crafts.Nutrition.CraftMinutes)
        {
            notes.Add($"Crafts.Nutrition.{nameof(Crafts.Nutrition.CraftMinutes)} was {Crafts.Nutrition.CraftMinutes}, clamped to {nutritionCraft}");
            Crafts.Nutrition.CraftMinutes = nutritionCraft;
        }

        var nutritionLevel = Math.Clamp(Crafts.Nutrition.UnitLevel, 1, 3);
        if (nutritionLevel != Crafts.Nutrition.UnitLevel)
        {
            notes.Add($"Crafts.Nutrition.{nameof(Crafts.Nutrition.UnitLevel)} was {Crafts.Nutrition.UnitLevel}, clamped to {nutritionLevel}");
            Crafts.Nutrition.UnitLevel = nutritionLevel;
        }

        if (Crafts.Nutrition.ReductionFactor < 1.0)
        {
            notes.Add($"Crafts.Nutrition.{nameof(Crafts.Nutrition.ReductionFactor)} was {Crafts.Nutrition.ReductionFactor}, clamped to 1.0");
            Crafts.Nutrition.ReductionFactor = 1.0;
        }

        if (!IsValidHex24(Crafts.Nutrition.EndProduct))
        {
            notes.Add($"Crafts.Nutrition.{nameof(Crafts.Nutrition.EndProduct)} '{Crafts.Nutrition.EndProduct}' was not a valid 24-character hex ID, reset to default");
            Crafts.Nutrition.EndProduct = "56742c2e4bdc2d95058b456d";
        }

        ItemConsumeReset ??= new();
        ItemConsumeReset.Items ??= [];
        Crafts.Shower.Ingredients ??= [];
        Crafts.Nutrition.Ingredients ??= [];

        return notes;
    }

    private static bool IsValidHex24(string? id)
    {
        return !string.IsNullOrWhiteSpace(id)
               && id.Length == 24
               && id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}

public class ActiveRecoveryConfig
{
    public bool Enabled { get; set; } = true;
    public bool BossKillResets { get; set; } = true;
    public int MinPmcKills { get; set; } = 5;
    public bool RequireSurvive { get; set; } = true;
    public bool Notify { get; set; } = true;
}

public class CraftsConfig
{
    public bool Enabled { get; set; } = true;
    public ShowerCraftConfig Shower { get; set; } = new();
    public NutritionCraftConfig Nutrition { get; set; } = new();
}

public class ShowerCraftConfig
{
    public int CraftMinutes { get; set; } = 30;
    public int LavatoryLevel { get; set; } = 3;
    public bool RequireFir { get; set; } = true;
    public List<string> Ingredients { get; set; } =
    [
        "5c13cd2486f774072c757944", // Soap
        "5d40412b86f7743cb332ac3a", // Shampoo
        "5d1b33a686f7742523398398", // Canister with purified water (superwater)
        "5e2af47786f7746d404f3aaa"  // Fleece fabric
    ];
    public string EndProduct { get; set; } = "5e2af47786f7746d404f3aaa"; // Fleece fabric
}

public class NutritionCraftConfig
{
    public int CraftMinutes { get; set; } = 15;
    public int UnitLevel { get; set; } = 1;
    public double ReductionFactor { get; set; } = 6.0;
    public bool RequireFir { get; set; } = true;
    public List<string> Ingredients { get; set; } =
    [
        "5751496424597720a27126da", // Can of Hot Rod energy
        "62a0a043cf4a99369e2624a5", // Bottle of OLOLO multivitamins
        "57347d7224597744596b4e72", // Can of beef stew (small)
        "56742c2e4bdc2d95058b456d"  // Zibbo lighter
    ];
    public string EndProduct { get; set; } = "56742c2e4bdc2d95058b456d"; // Zibbo lighter
}

public class ItemConsumeConfig
{
    public bool Enabled { get; set; } = true;
    public bool RequireFoundInRaid { get; set; } = true;
    public List<string> Items { get; set; } = [];
}
