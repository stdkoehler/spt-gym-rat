namespace GymRat.Tests;

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

        Expect("shipped activeRecovery.enabled matches the code default", cfg.ActiveRecovery.Enabled == defaults.ActiveRecovery.Enabled);
        Expect("shipped activeRecovery.bossKillResets matches the code default", cfg.ActiveRecovery.BossKillResets == defaults.ActiveRecovery.BossKillResets);
        Expect("shipped activeRecovery.minPmcKills matches the code default", cfg.ActiveRecovery.MinPmcKills == defaults.ActiveRecovery.MinPmcKills);
        Expect("shipped activeRecovery.requireSurvive matches the code default", cfg.ActiveRecovery.RequireSurvive == defaults.ActiveRecovery.RequireSurvive);
        Expect("shipped activeRecovery.notify matches the code default", cfg.ActiveRecovery.Notify == defaults.ActiveRecovery.Notify);

        Expect("shipped crafts.enabled matches the code default", cfg.Crafts.Enabled == defaults.Crafts.Enabled);
        Expect("shipped crafts.shower.craftMinutes matches the code default", cfg.Crafts.Shower.CraftMinutes == defaults.Crafts.Shower.CraftMinutes);
        Expect("shipped crafts.shower.lavatoryLevel matches the code default", cfg.Crafts.Shower.LavatoryLevel == defaults.Crafts.Shower.LavatoryLevel);
        Expect("shipped crafts.shower.requireFir matches the code default", cfg.Crafts.Shower.RequireFir == defaults.Crafts.Shower.RequireFir);
        Expect("shipped crafts.shower.ingredients matches the code default", cfg.Crafts.Shower.Ingredients.SequenceEqual(defaults.Crafts.Shower.Ingredients));
        Expect("shipped crafts.shower.endProduct matches the code default", cfg.Crafts.Shower.EndProduct == defaults.Crafts.Shower.EndProduct);

        Expect("shipped crafts.nutrition.craftMinutes matches the code default", cfg.Crafts.Nutrition.CraftMinutes == defaults.Crafts.Nutrition.CraftMinutes);
        Expect("shipped crafts.nutrition.unitLevel matches the code default", cfg.Crafts.Nutrition.UnitLevel == defaults.Crafts.Nutrition.UnitLevel);
        Expect("shipped crafts.nutrition.reductionFactor matches the code default", Math.Abs(cfg.Crafts.Nutrition.ReductionFactor - defaults.Crafts.Nutrition.ReductionFactor) < 0.001);
        Expect("shipped crafts.nutrition.requireFir matches the code default", cfg.Crafts.Nutrition.RequireFir == defaults.Crafts.Nutrition.RequireFir);
        Expect("shipped crafts.nutrition.ingredients matches the code default", cfg.Crafts.Nutrition.Ingredients.SequenceEqual(defaults.Crafts.Nutrition.Ingredients));
        Expect("shipped crafts.nutrition.endProduct matches the code default", cfg.Crafts.Nutrition.EndProduct == defaults.Crafts.Nutrition.EndProduct);

        Expect("fleece is in default shower ingredients", defaults.Crafts.Shower.Ingredients.Contains("5e2af47786f7746d404f3aaa"));
        Expect("shampoo is in default shower ingredients", defaults.Crafts.Shower.Ingredients.Contains("5d40412b86f7743cb332ac3a"));
        Expect("superwater is in default shower ingredients", defaults.Crafts.Shower.Ingredients.Contains("5d1b33a686f7742523398398"));
        Expect("fleece is default shower end product", defaults.Crafts.Shower.EndProduct == "5e2af47786f7746d404f3aaa");
        Expect("vitamins is in default nutrition ingredients", defaults.Crafts.Nutrition.Ingredients.Contains("62a0a043cf4a99369e2624a5"));
        Expect("zibbo is in default nutrition ingredients", defaults.Crafts.Nutrition.Ingredients.Contains("56742c2e4bdc2d95058b456d"));
        Expect("zibbo is default nutrition end product", defaults.Crafts.Nutrition.EndProduct == "56742c2e4bdc2d95058b456d");

        Expect("shipped itemConsumeReset.enabled matches the code default", cfg.ItemConsumeReset.Enabled == defaults.ItemConsumeReset.Enabled);
        Expect("shipped itemConsumeReset.requireFoundInRaid matches the code default", cfg.ItemConsumeReset.RequireFoundInRaid == defaults.ItemConsumeReset.RequireFoundInRaid);
        Expect("shipped itemConsumeReset.items matches the code default", cfg.ItemConsumeReset.Items.SequenceEqual(defaults.ItemConsumeReset.Items));
        Expect("items is empty by default", defaults.ItemConsumeReset.Items.Count == 0);
        Expect("requireFoundInRaid defaults to true", defaults.ItemConsumeReset.RequireFoundInRaid);

        var high = new Config
        {
            GymResetMinutes = 999999,
            Crafts = new CraftsConfig
            {
                Shower = new ShowerCraftConfig
                {
                    CraftMinutes = 999999,
                    LavatoryLevel = 10
                },
                Nutrition = new NutritionCraftConfig
                {
                    CraftMinutes = 999999,
                    UnitLevel = 10
                }
            }
        };
        var notes = high.Sanitise();
        Expect("too-large gymResetMinutes is clamped", high.GymResetMinutes == Config.MaxResetMinutes);
        Expect("too-large showerCraftMinutes is clamped", high.Crafts.Shower.CraftMinutes == Config.MaxResetMinutes);
        Expect("too-large showerLavatoryLevel is clamped", high.Crafts.Shower.LavatoryLevel == 3);
        Expect("too-large nutritionCraftMinutes is clamped", high.Crafts.Nutrition.CraftMinutes == Config.MaxResetMinutes);
        Expect("too-large nutritionUnitLevel is clamped", high.Crafts.Nutrition.UnitLevel == 3);
        Expect("high config generated 5 notes", notes.Count == 5);

        var low = new Config
        {
            GymResetMinutes = 0,
            ActiveRecovery = new ActiveRecoveryConfig
            {
                MinPmcKills = -5
            },
            Crafts = new CraftsConfig
            {
                Shower = new ShowerCraftConfig
                {
                    CraftMinutes = 0,
                    LavatoryLevel = 0,
                    EndProduct = "not-a-hex"
                },
                Nutrition = new NutritionCraftConfig
                {
                    CraftMinutes = 0,
                    UnitLevel = 0,
                    ReductionFactor = 0.5,
                    EndProduct = "short"
                }
            },
            ItemConsumeReset = new ItemConsumeConfig
            {
                Items = null!
            }
        };
        var lowNotes = low.Sanitise();
        Expect("zero gymResetMinutes is clamped", low.GymResetMinutes == Config.MinResetMinutes);
        Expect("negative minPmcKills is clamped to zero", low.ActiveRecovery.MinPmcKills == 0);
        Expect("zero showerCraftMinutes is clamped", low.Crafts.Shower.CraftMinutes == 1);
        Expect("zero showerLavatoryLevel is clamped", low.Crafts.Shower.LavatoryLevel == 1);
        Expect("zero nutritionCraftMinutes is clamped", low.Crafts.Nutrition.CraftMinutes == 1);
        Expect("zero nutritionUnitLevel is clamped", low.Crafts.Nutrition.UnitLevel == 1);
        Expect("sub-1.0 nutritionReductionFactor is clamped to 1.0", Math.Abs(low.Crafts.Nutrition.ReductionFactor - 1.0) < 0.001);
        Expect("invalid showerEndProduct reset to default", low.Crafts.Shower.EndProduct == defaults.Crafts.Shower.EndProduct);
        Expect("invalid nutritionEndProduct reset to default", low.Crafts.Nutrition.EndProduct == defaults.Crafts.Nutrition.EndProduct);
        Expect("null items is initialized to empty", low.ItemConsumeReset.Items != null && low.ItemConsumeReset.Items.Count == 0);
        Expect("low config generated 9 notes", lowNotes.Count == 9);

        // Test null sections recovery
        var nullSections = new Config
        {
            ActiveRecovery = null!,
            Crafts = null!,
            ItemConsumeReset = null!
        };
        nullSections.Sanitise();
        Expect("null ActiveRecovery is re-initialized", nullSections.ActiveRecovery != null);
        Expect("null Crafts is re-initialized", nullSections.Crafts != null && nullSections.Crafts.Shower != null && nullSections.Crafts.Nutrition != null);
        Expect("null ItemConsumeReset is re-initialized", nullSections.ItemConsumeReset != null && nullSections.ItemConsumeReset.Items != null);

        Console.WriteLine($"  {pass} passed, {fail} failed");
        return (pass, fail);
    }
}
