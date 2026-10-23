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

    /// <summary>Active recovery resets gym muscle pain when completing strenuous combat feats in raid.</summary>
    public bool ActiveRecoveryEnabled { get; set; } = true;

    /// <summary>Eliminating a boss in a raid clears gym muscle pain.</summary>
    public bool BossKillResets { get; set; } = true;

    /// <summary>Number of PMC kills in a raid required to clear muscle pain (0 = disabled).</summary>
    public int MinPmcKillsForReset { get; set; } = 5;

    /// <summary>Require surviving the raid to trigger active recovery.</summary>
    public bool RequireSurviveForActiveRecovery { get; set; } = true;

    /// <summary>Send an in-game notification message from Therapist when active recovery triggers.</summary>
    public bool NotifyOnActiveRecovery { get; set; } = true;

    /// <summary>Enable Hideout recovery crafts (Recovery Shower and Nutrition Regime).</summary>
    public bool HideoutCraftsEnabled { get; set; } = true;

    /// <summary>Craft duration in minutes for the Recovery Shower craft in the Lavatory.</summary>
    public int ShowerCraftMinutes { get; set; } = 30;

    /// <summary>Required Lavatory level for the Recovery Shower craft (1 to 3).</summary>
    public int ShowerLavatoryLevel { get; set; } = 3;

    /// <summary>Require FIR items for the Recovery Shower craft.</summary>
    public bool ShowerRequireFir { get; set; } = true;

    /// <summary>Ingredients for the Recovery Shower craft (item template IDs). Each requires 1 unit.</summary>
    public List<string> ShowerIngredients { get; set; } =
    [
        "5c13cd2486f774072c757944", // Soap
        "5d40412b86f7743cb332ac3a", // Shampoo
        "5d1b33a686f7742523398398", // Canister with purified water (superwater)
        "5e2af47786f7746d404f3aaa"  // Fleece fabric
    ];

    /// <summary>End product item template ID for the Recovery Shower craft.</summary>
    public string ShowerEndProduct { get; set; } = "5e2af47786f7746d404f3aaa"; // Fleece fabric

    /// <summary>Craft duration in minutes for the Nutrition Regime craft in the Nutrition Unit.</summary>
    public int NutritionCraftMinutes { get; set; } = 15;

    /// <summary>Required Nutrition Unit level for the Nutrition Regime craft (1 to 3).</summary>
    public int NutritionUnitLevel { get; set; } = 1;

    /// <summary>Factor by which the Nutrition Regime craft reduces remaining workout muscle pain (e.g. 6 = divided by 6).</summary>
    public double NutritionReductionFactor { get; set; } = 6.0;

    /// <summary>Require FIR items for the Nutrition Regime craft.</summary>
    public bool NutritionRequireFir { get; set; } = true;

    /// <summary>Ingredients for the Nutrition Regime craft (item template IDs). Each requires 1 unit.</summary>
    public List<string> NutritionIngredients { get; set; } =
    [
        "5751496424597720a27126da", // Can of Hot Rod energy
        "62a0a043cf4a99369e2624a5", // Bottle of OLOLO multivitamins
        "57347d7224597744596b4e72", // Can of beef stew (small)
        "56742c2e4bdc2d95058b456d"  // Zibbo lighter
    ];

    /// <summary>End product item template ID for the Nutrition Regime craft.</summary>
    public string NutritionEndProduct { get; set; } = "56742c2e4bdc2d95058b456d"; // Zibbo lighter

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

        if (MinPmcKillsForReset < 0)
        {
            notes.Add($"{nameof(MinPmcKillsForReset)} was {MinPmcKillsForReset}, clamped to 0");
            MinPmcKillsForReset = 0;
        }

        var showerCraft = Math.Clamp(ShowerCraftMinutes, 1, MaxResetMinutes);
        if (showerCraft != ShowerCraftMinutes)
        {
            notes.Add($"{nameof(ShowerCraftMinutes)} was {ShowerCraftMinutes}, clamped to {showerCraft}");
            ShowerCraftMinutes = showerCraft;
        }

        var showerLevel = Math.Clamp(ShowerLavatoryLevel, 1, 3);
        if (showerLevel != ShowerLavatoryLevel)
        {
            notes.Add($"{nameof(ShowerLavatoryLevel)} was {ShowerLavatoryLevel}, clamped to {showerLevel}");
            ShowerLavatoryLevel = showerLevel;
        }

        var nutritionCraft = Math.Clamp(NutritionCraftMinutes, 1, MaxResetMinutes);
        if (nutritionCraft != NutritionCraftMinutes)
        {
            notes.Add($"{nameof(NutritionCraftMinutes)} was {NutritionCraftMinutes}, clamped to {nutritionCraft}");
            NutritionCraftMinutes = nutritionCraft;
        }

        var nutritionLevel = Math.Clamp(NutritionUnitLevel, 1, 3);
        if (nutritionLevel != NutritionUnitLevel)
        {
            notes.Add($"{nameof(NutritionUnitLevel)} was {NutritionUnitLevel}, clamped to {nutritionLevel}");
            NutritionUnitLevel = nutritionLevel;
        }

        if (NutritionReductionFactor < 1.0)
        {
            notes.Add($"{nameof(NutritionReductionFactor)} was {NutritionReductionFactor}, clamped to 1.0");
            NutritionReductionFactor = 1.0;
        }

        if (!IsValidHex24(ShowerEndProduct))
        {
            notes.Add($"{nameof(ShowerEndProduct)} '{ShowerEndProduct}' was not a valid 24-character hex ID, reset to default");
            ShowerEndProduct = "5e2af47786f7746d404f3aaa";
        }

        if (!IsValidHex24(NutritionEndProduct))
        {
            notes.Add($"{nameof(NutritionEndProduct)} '{NutritionEndProduct}' was not a valid 24-character hex ID, reset to default");
            NutritionEndProduct = "56742c2e4bdc2d95058b456d";
        }

        ShowerIngredients ??= [];
        NutritionIngredients ??= [];

        return notes;
    }

    private static bool IsValidHex24(string? id)
    {
        return !string.IsNullOrWhiteSpace(id)
               && id.Length == 24
               && id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}
