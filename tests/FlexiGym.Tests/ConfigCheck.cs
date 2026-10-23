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

        Expect("shipped hideoutCraftsEnabled matches the code default", cfg.HideoutCraftsEnabled == defaults.HideoutCraftsEnabled);
        Expect("shipped showerCraftMinutes matches the code default", cfg.ShowerCraftMinutes == defaults.ShowerCraftMinutes);
        Expect("shipped showerLavatoryLevel matches the code default", cfg.ShowerLavatoryLevel == defaults.ShowerLavatoryLevel);
        Expect("shipped showerRequireFir matches the code default", cfg.ShowerRequireFir == defaults.ShowerRequireFir);
        Expect("shipped showerIngredients matches the code default", cfg.ShowerIngredients.SequenceEqual(defaults.ShowerIngredients));
        Expect("shipped showerEndProduct matches the code default", cfg.ShowerEndProduct == defaults.ShowerEndProduct);

        Expect("shipped nutritionCraftMinutes matches the code default", cfg.NutritionCraftMinutes == defaults.NutritionCraftMinutes);
        Expect("shipped nutritionUnitLevel matches the code default", cfg.NutritionUnitLevel == defaults.NutritionUnitLevel);
        Expect("shipped nutritionReductionFactor matches the code default", Math.Abs(cfg.NutritionReductionFactor - defaults.NutritionReductionFactor) < 0.001);
        Expect("shipped nutritionRequireFir matches the code default", cfg.NutritionRequireFir == defaults.NutritionRequireFir);
        Expect("shipped nutritionIngredients matches the code default", cfg.NutritionIngredients.SequenceEqual(defaults.NutritionIngredients));
        Expect("shipped nutritionEndProduct matches the code default", cfg.NutritionEndProduct == defaults.NutritionEndProduct);

        Expect("fleece is in default shower ingredients", defaults.ShowerIngredients.Contains("5e2af47786f7746d404f3aaa"));
        Expect("shampoo is in default shower ingredients", defaults.ShowerIngredients.Contains("5d40412b86f7743cb332ac3a"));
        Expect("superwater is in default shower ingredients", defaults.ShowerIngredients.Contains("5d1b33a686f7742523398398"));
        Expect("fleece is default shower end product", defaults.ShowerEndProduct == "5e2af47786f7746d404f3aaa");
        Expect("vitamins is in default nutrition ingredients", defaults.NutritionIngredients.Contains("62a0a043cf4a99369e2624a5"));
        Expect("zibbo is in default nutrition ingredients", defaults.NutritionIngredients.Contains("56742c2e4bdc2d95058b456d"));
        Expect("zibbo is default nutrition end product", defaults.NutritionEndProduct == "56742c2e4bdc2d95058b456d");

        var high = new Config
        {
            GymResetMinutes = 999999,
            ShowerCraftMinutes = 999999,
            ShowerLavatoryLevel = 10,
            NutritionCraftMinutes = 999999,
            NutritionUnitLevel = 10
        };
        var notes = high.Sanitise();
        Expect("too-large gymResetMinutes is clamped", high.GymResetMinutes == Config.MaxResetMinutes);
        Expect("too-large showerCraftMinutes is clamped", high.ShowerCraftMinutes == Config.MaxResetMinutes);
        Expect("too-large showerLavatoryLevel is clamped", high.ShowerLavatoryLevel == 3);
        Expect("too-large nutritionCraftMinutes is clamped", high.NutritionCraftMinutes == Config.MaxResetMinutes);
        Expect("too-large nutritionUnitLevel is clamped", high.NutritionUnitLevel == 3);
        Expect("high config generated 5 notes", notes.Count == 5);

        var low = new Config
        {
            GymResetMinutes = 0,
            MinPmcKillsForReset = -5,
            ShowerCraftMinutes = 0,
            ShowerLavatoryLevel = 0,
            NutritionCraftMinutes = 0,
            NutritionUnitLevel = 0,
            NutritionReductionFactor = 0.5,
            ShowerEndProduct = "not-a-hex",
            NutritionEndProduct = "short"
        };
        var lowNotes = low.Sanitise();
        Expect("zero gymResetMinutes is clamped", low.GymResetMinutes == Config.MinResetMinutes);
        Expect("negative minPmcKills is clamped to zero", low.MinPmcKillsForReset == 0);
        Expect("zero showerCraftMinutes is clamped", low.ShowerCraftMinutes == 1);
        Expect("zero showerLavatoryLevel is clamped", low.ShowerLavatoryLevel == 1);
        Expect("zero nutritionCraftMinutes is clamped", low.NutritionCraftMinutes == 1);
        Expect("zero nutritionUnitLevel is clamped", low.NutritionUnitLevel == 1);
        Expect("sub-1.0 nutritionReductionFactor is clamped to 1.0", Math.Abs(low.NutritionReductionFactor - 1.0) < 0.001);
        Expect("invalid showerEndProduct reset to default", low.ShowerEndProduct == defaults.ShowerEndProduct);
        Expect("invalid nutritionEndProduct reset to default", low.NutritionEndProduct == defaults.NutritionEndProduct);
        Expect("low config generated 9 notes", lowNotes.Count == 9);

        Console.WriteLine($"  {pass} passed, {fail} failed");
        return (pass, fail);
    }
}
