using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.DI.Routing;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Bot;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Request;
using SPTarkov.Server.Core.Models.Eft.Health;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Routers.ItemEvents;
using SPTarkov.Server.Core.Routers.Static;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Servers.Http;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;
using BotBaseHealth = SPTarkov.Server.Core.Models.Eft.Common.Tables.BotBaseHealth;

namespace FlexiGym;

/// <summary>
/// The gym "reset" is the muscle pain applied when a workout finishes; its duration comes from
/// the finishEffect of every entry in hideout/qte.json (vanilla: 86400s).
///  1. Rewriting that duration once at load makes server and client use the configured time.
///  2. Consuming a configured item clears the muscle pain at once. The server's "Eat" route is
///     wrapped for this (see HookEatRoute); if that fails the mod keeps working without it.
///  3. Active recovery clears muscle pain upon completing a raid with a boss kill or >=5 PMC kills
///     (configurable), followed by an in-game notification from Therapist.
///  4. Hideout recovery crafts (Recovery Shower and Nutrition Regime) allow players to clear or reduce
///     workout muscle pain directly in the hideout upon collecting items (HookTakeProductionRoute).
/// A broken config disables the mod instead of crashing the server.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad)]
public class FlexiGymMod(
    ISptLogger<FlexiGymMod> logger,
    ModHelper modHelper,
    HideoutTable hideoutTable,
    ItemEventCallbacks itemEventCallbacks,
    HttpServer httpServer,
    ProfileHelper profileHelper,
    BotHelper botHelper,
    MailSendService mailSendService,
    SaveServer saveServer) : IOnLoad
{
    public static readonly MongoId ShowerRecipeId = new("66f1e8100000000000000001");
    public static readonly MongoId NutritionRecipeId = new("66f1e8100000000000000002");

    private const string Tag = "[FlexiGym]";
    private const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;

    private string ModFolder => modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Config config;
        try
        {
            config = Config.Load(Path.Join(ModFolder, "config.jsonc"));
        }
        catch (Exception ex)
        {
            logger.Error($"{Tag} Could not read config.jsonc. Mod disabled.", ex);
            return Task.CompletedTask;
        }

        foreach (var note in config.Sanitise()) logger.Warning($"{Tag} config: {note}");

        if (!config.Enabled)
        {
            logger.Info($"{Tag} Disabled via config.");
            return Task.CompletedTask;
        }

        var patched = PatchResetTime(config.GymResetMinutes * 60L);
        logger.Success($"{Tag} Gym resets every {config.GymResetMinutes} min ({patched} workouts patched)");

        if (config.ResetItems.Count > 0)
        {
            try
            {
                HookEatRoute(config);
                logger.Info($"{Tag} {config.ResetItems.Count} item(s) reset the gym when consumed" +
                            (config.RequireFoundInRaid ? " (FIR only)" : ""));
            }
            catch (Exception ex)
            {
                logger.Error($"{Tag} Could not hook item consumption; item reset disabled.", ex);
            }
        }

        if (config.ActiveRecoveryEnabled)
        {
            try
            {
                HookRaidEnd(config);
                var triggers = new List<string>();
                if (config.BossKillResets) triggers.Add("Boss kill");
                if (config.MinPmcKillsForReset > 0) triggers.Add($">={config.MinPmcKillsForReset} PMC kills");
                logger.Info($"{Tag} Active recovery enabled ({string.Join(", ", triggers)}" +
                            (config.RequireSurviveForActiveRecovery ? ", survive required" : "") +
                            (config.NotifyOnActiveRecovery ? ", Therapist mail alert" : "") + ")");
            }
            catch (Exception ex)
            {
                logger.Error($"{Tag} Could not hook raid end; active recovery disabled.", ex);
            }
        }

        if (config.HideoutCraftsEnabled)
        {
            try
            {
                RegisterHideoutCrafts(config);
                HookTakeProductionRoute(config);
                logger.Info($"{Tag} Hideout recovery crafts enabled (Recovery Shower in Lavatory lvl {config.ShowerLavatoryLevel}, " +
                            $"Nutrition Regime in Nutrition Unit lvl {config.NutritionUnitLevel})");
            }
            catch (Exception ex)
            {
                logger.Error($"{Tag} Could not register hideout recovery crafts; hideout crafts disabled.", ex);
            }
        }

        return Task.CompletedTask;
    }

    private int PatchResetTime(long seconds)
    {
        var patched = 0;

        foreach (var qte in hideoutTable.Qte)
        {
            if (qte.Results is null || !qte.Results.TryGetValue(QteEffectType.finishEffect, out var finish)) continue;

            foreach (var effect in finish.RewardEffects ?? [])
            {
                if (effect.Type != QteRewardType.MusclePain) continue;
                effect.Time = seconds;
                patched++;
            }
        }

        return patched;
    }

    /// <summary>
    /// SPT picks the first router that can handle an action and offers no hook for it, so the live
    /// "Eat" route of HealthItemEventRouter is wrapped in place (reflection on the router instance
    /// that ItemEventCallbacks actually uses).
    /// </summary>
    private void HookEatRoute(Config config)
    {
        var routers = typeof(ItemEventCallbacks).GetFields(Fields)
            .Select(f => f.GetValue(itemEventCallbacks))
            .OfType<IEnumerable<ItemEventRouter>>()
            .FirstOrDefault() ?? throw new InvalidOperationException("router list not found");

        var health = routers.OfType<HealthItemEventRouter>().FirstOrDefault()
                     ?? throw new InvalidOperationException("health router not found");

        var eatRoute = typeof(ItemEventRouter).GetFields(Fields)
            .Select(f => f.GetValue(health))
            .OfType<IEnumerable<ItemRouteAction>>()
            .SelectMany(r => r)
            .FirstOrDefault(r => r.Url == "Eat") ?? throw new InvalidOperationException("Eat route not found");

        var items = new HashSet<string>(config.ResetItems, StringComparer.OrdinalIgnoreCase);
        var original = eatRoute.Action;

        var wrapped = new Func<string, PmcData, BaseInteractionRequestData, MongoId, ItemEventRouterResponse,
            CancellationToken, ValueTask<ItemEventRouterResponse>>(
            async (url, pmcData, body, sessionId, output, ct) =>
            {
                // Look the item up first: a fully consumed item is gone once the original ran.
                var trigger = false;
                try
                {
                    var item = body is OffraidEatRequestData request
                        ? pmcData.Inventory?.Items?.FirstOrDefault(i => i.Id == request.Item)
                        : null;
                    trigger = item is not null
                              && items.Contains(item.Template.ToString())
                              && (!config.RequireFoundInRaid || item.Upd?.SpawnedInSession == true);
                }
                catch (Exception ex)
                {
                    logger.Error($"{Tag} Could not inspect consumed item.", ex);
                }

                var result = await original(url, pmcData, body, sessionId, output, ct);

                if (trigger)
                {
                    ClearMusclePain(pmcData.Health);
                    SyncHealthChanges(result, sessionId, pmcData);
                }
                return result;
            });

        // Action is an init-only record property; the setter is still reachable via reflection.
        typeof(ItemRouteAction).GetProperty(nameof(ItemRouteAction.Action))!.SetMethod!.Invoke(eatRoute, [wrapped]);
    }

    /// <summary>
    /// Hooks MatchStaticRouter's OnAfterAction event to detect raid completion and apply active gym recovery.
    /// </summary>
    private void HookRaidEnd(Config config)
    {
        var matchRouter = FindMatchStaticRouter(httpServer)
                          ?? throw new InvalidOperationException("MatchStaticRouter not found in HttpServer");

        matchRouter.OnAfterAction += async (_, args) =>
        {
            try
            {
                if (args is not StaticDynamicOnAfterEventRequestData afterData) return;
                if (afterData.Url != "/client/match/local/end") return;
                if (afterData.RequestData is not EndLocalRaidRequestData raidEnd) return;

                await HandleActiveRecoveryAsync(raidEnd, afterData.SessionId, config);
            }
            catch (Exception ex)
            {
                logger.Error($"{Tag} Error processing post-raid active recovery.", ex);
            }
        };
    }

    private static MatchStaticRouter? FindMatchStaticRouter(HttpServer server)
    {
        var listeners = typeof(HttpServer).GetFields(Fields)
            .Select(f => f.GetValue(server))
            .OfType<IEnumerable<IHttpListener>>()
            .FirstOrDefault();
        if (listeners is null) return null;

        var sptListener = listeners.OfType<SptHttpListener>().FirstOrDefault();
        if (sptListener is null) return null;

        var httpRouter = typeof(SptHttpListener).GetFields(Fields)
            .Select(f => f.GetValue(sptListener))
            .OfType<HttpRouter>()
            .FirstOrDefault();
        if (httpRouter is null) return null;

        var staticRouters = typeof(HttpRouter).GetFields(Fields)
            .Select(f => f.GetValue(httpRouter))
            .OfType<IEnumerable<StaticRouter>>()
            .FirstOrDefault();
        if (staticRouters is null) return null;

        return staticRouters.OfType<MatchStaticRouter>().FirstOrDefault();
    }

    private async Task HandleActiveRecoveryAsync(EndLocalRaidRequestData raidEnd, MongoId sessionId, Config config)
    {
        if (!config.ActiveRecoveryEnabled) return;

        var serverIdParts = raidEnd.ServerId?.Split('.') ?? [];
        var isPmc = (serverIdParts.Length > 1 && serverIdParts[1].Contains("pmc", StringComparison.OrdinalIgnoreCase))
                    || (raidEnd.Results?.Profile?.Info?.Side is not null
                        && !string.Equals(raidEnd.Results.Profile.Info.Side, "Savage", StringComparison.OrdinalIgnoreCase));
        if (!isPmc) return;

        var survived = raidEnd.Results is not null
                       && (raidEnd.Results.IsPlayerSurvived() || raidEnd.Results.IsMapToMapTransfer());
        if (config.RequireSurviveForActiveRecovery && !survived) return;

        var victims = raidEnd.Results?.Profile?.Stats?.Eft?.Victims ?? [];
        var bossKilled = false;
        var pmcKills = 0;

        foreach (var victim in victims)
        {
            if (!string.IsNullOrEmpty(victim.Role) && botHelper.IsBotBoss(victim.Role))
            {
                bossKilled = true;
            }
            else if (botHelper.IsBotPmc(victim.Role)
                     || string.Equals(victim.Side, "Usec", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(victim.Side, "Bear", StringComparison.OrdinalIgnoreCase))
            {
                pmcKills++;
            }
        }

        var bossReset = config.BossKillResets && bossKilled;
        var pmcReset = config.MinPmcKillsForReset > 0 && pmcKills >= config.MinPmcKillsForReset;

        if (!bossReset && !pmcReset) return;

        var pmcData = profileHelper.GetPmcProfile(sessionId);
        if (pmcData is null || !HasMusclePain(pmcData.Health)) return;

        ClearMusclePain(pmcData.Health);
        await saveServer.SaveProfileAsync(sessionId);
        logger.Success($"{Tag} Active recovery triggered for player {sessionId}! (Boss: {bossKilled}, PMCs: {pmcKills})");

        if (config.NotifyOnActiveRecovery)
        {
            var message = BuildRecoveryMessage(bossReset, pmcReset, pmcKills);
            mailSendService.SendDirectNpcMessageToPlayer(
                sessionId,
                Traders.THERAPIST.ToString(),
                MessageType.NpcTraderMessage,
                message,
                null);
        }
    }

    private static string BuildRecoveryMessage(bool bossReset, bool pmcReset, int pmcKills)
    {
        if (bossReset && pmcReset)
        {
            return $"Hello, sweetie. I heard you eliminated a notorious boss and neutralized {pmcKills} PMCs out there. Combat of that intensity pushed your body to its limit and completely worked out your muscle stiffness. Your muscles have fully recovered — you're ready for another workout.";
        }

        if (bossReset)
        {
            return "Hello, sweetie. Word came back that you took down a boss out there. Pushing through combat with that level of adrenaline naturally works out lactic acid buildup. Your muscle stiffness has completely cleared up. Take care of yourself out there.";
        }

        return $"Hello, sweetie. Word came back that you took down {pmcKills} PMCs during your raid. That level of exertion has flushed the lactic acid right out of your system. Your muscle soreness has completely cleared up — you're ready to hit the gym again.";
    }

    /// <summary>
    /// Injects the Recovery Shower and Nutrition Regime crafting recipes into HideoutTable.Production.Recipes.
    /// </summary>
    private void RegisterHideoutCrafts(Config config)
    {
        if (hideoutTable.Production?.Recipes is null)
        {
            logger.Warning($"{Tag} HideoutTable.Production.Recipes is null; cannot inject recovery crafts.");
            return;
        }

        hideoutTable.Production.Recipes.RemoveAll(r => r.Id == ShowerRecipeId || r.Id == NutritionRecipeId);

        var showerRecipe = new HideoutProduction
        {
            Id = ShowerRecipeId,
            AreaType = HideoutAreas.WaterCloset,
            ProductionTime = config.ShowerCraftMinutes * 60.0,
            EndProduct = new MongoId(config.ShowerEndProduct),
            Count = 1,
            Continuous = false,
            Locked = false,
            NeedFuelForAllProductionTime = false,
            Requirements =
            [
                new Requirement
                {
                    Type = RequirementType.Area.ToString(),
                    AreaType = (int)HideoutAreas.WaterCloset,
                    RequiredLevel = config.ShowerLavatoryLevel
                },
                ..config.ShowerIngredients.Select(id => new Requirement
                {
                    Type = RequirementType.Item.ToString(),
                    TemplateId = new MongoId(id),
                    Count = 1,
                    IsSpawnedInSession = config.ShowerRequireFir
                })
            ]
        };

        var nutritionRecipe = new HideoutProduction
        {
            Id = NutritionRecipeId,
            AreaType = HideoutAreas.Kitchen,
            ProductionTime = config.NutritionCraftMinutes * 60.0,
            EndProduct = new MongoId(config.NutritionEndProduct),
            Count = 1,
            Continuous = false,
            Locked = false,
            NeedFuelForAllProductionTime = false,
            Requirements =
            [
                new Requirement
                {
                    Type = RequirementType.Area.ToString(),
                    AreaType = (int)HideoutAreas.Kitchen,
                    RequiredLevel = config.NutritionUnitLevel
                },
                ..config.NutritionIngredients.Select(id => new Requirement
                {
                    Type = RequirementType.Item.ToString(),
                    TemplateId = new MongoId(id),
                    Count = 1,
                    IsSpawnedInSession = config.NutritionRequireFir
                })
            ]
        };

        hideoutTable.Production.Recipes.Add(showerRecipe);
        hideoutTable.Production.Recipes.Add(nutritionRecipe);
    }

    /// <summary>
    /// Wraps HideoutTakeProduction in HideoutItemEventRouter to intercept recipe collection and apply workout resets.
    /// </summary>
    private void HookTakeProductionRoute(Config config)
    {
        var routers = typeof(ItemEventCallbacks).GetFields(Fields)
            .Select(f => f.GetValue(itemEventCallbacks))
            .OfType<IEnumerable<ItemEventRouter>>()
            .FirstOrDefault() ?? throw new InvalidOperationException("router list not found");

        var hideoutRouter = routers.OfType<HideoutItemEventRouter>().FirstOrDefault()
                            ?? throw new InvalidOperationException("hideout router not found");

        var takeProductionRoute = typeof(ItemEventRouter).GetFields(Fields)
            .Select(f => f.GetValue(hideoutRouter))
            .OfType<IEnumerable<ItemRouteAction>>()
            .SelectMany(r => r)
            .FirstOrDefault(r => r.Url == "HideoutTakeProduction")
            ?? throw new InvalidOperationException("HideoutTakeProduction route not found");

        var original = takeProductionRoute.Action;

        var wrapped = new Func<string, PmcData, BaseInteractionRequestData, MongoId, ItemEventRouterResponse,
            CancellationToken, ValueTask<ItemEventRouterResponse>>(
            async (url, pmcData, body, sessionId, output, ct) =>
            {
                var result = await original(url, pmcData, body, sessionId, output, ct);

                try
                {
                    if (body is HideoutTakeProductionRequestData takeRequest)
                    {
                        await HandleHideoutCraftTakeAsync(takeRequest.RecipeId, pmcData, sessionId, result, config);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"{Tag} Error processing hideout craft completion.", ex);
                }

                return result;
            });

        typeof(ItemRouteAction).GetProperty(nameof(ItemRouteAction.Action))!.SetMethod!.Invoke(takeProductionRoute, [wrapped]);
    }

    private async Task HandleHideoutCraftTakeAsync(MongoId recipeId, PmcData pmcData, MongoId sessionId,
        ItemEventRouterResponse result, Config config)
    {
        if (result.Warnings is not null && result.Warnings.Count > 0) return;

        if (recipeId == ShowerRecipeId)
        {
            ApplyCraftRewardFir(result, pmcData, sessionId, config.ShowerEndProduct, config.ShowerRequireFir);

            var reset = false;
            if (HasMusclePain(pmcData.Health))
            {
                ClearMusclePain(pmcData.Health);
                SyncHealthChanges(result, sessionId, pmcData);
                reset = true;
                logger.Success($"{Tag} Recovery Shower taken: muscle pain fully reset for player {sessionId}!");
            }

            await saveServer.SaveProfileAsync(sessionId);
            if (!reset)
            {
                logger.Info($"{Tag} Recovery Shower taken by player {sessionId} (FIR: {config.ShowerRequireFir}).");
            }
        }
        else if (recipeId == NutritionRecipeId)
        {
            ApplyCraftRewardFir(result, pmcData, sessionId, config.NutritionEndProduct, config.NutritionRequireFir);

            var reduced = false;
            if (HasMusclePain(pmcData.Health))
            {
                ReduceMusclePain(pmcData.Health, config.NutritionReductionFactor);
                SyncHealthChanges(result, sessionId, pmcData);
                reduced = true;
                logger.Success($"{Tag} Nutrition Regime taken: muscle pain reduced by factor {config.NutritionReductionFactor:0.#} for player {sessionId}!");
            }

            await saveServer.SaveProfileAsync(sessionId);
            if (!reduced)
            {
                logger.Info($"{Tag} Nutrition Regime taken by player {sessionId} (FIR: {config.NutritionRequireFir}).");
            }
        }
    }

    /// <summary>
    /// Synchronizes the Found-In-Raid status of the produced item with the recipe's FIR requirement.
    /// If the recipe requires FIR ingredients, the returned item is FIR; otherwise it is non-FIR.
    /// </summary>
    private static void ApplyCraftRewardFir(ItemEventRouterResponse response, PmcData pmcData, MongoId sessionId,
        string endProductTpl, bool isFir)
    {
        if (response.ProfileChanges?.TryGetValue(sessionId, out var changes) != true || changes?.Items?.NewItems is null)
        {
            return;
        }

        var endTpl = new MongoId(endProductTpl);
        var targetItems = changes.Items.NewItems
            .Where(i => i.Template == endTpl)
            .ToList();

        foreach (var newItem in targetItems)
        {
            newItem.AddUpd();
            if (newItem.Upd is not null) newItem.Upd.SpawnedInSession = isFir;

            var pmcItem = pmcData.Inventory?.Items?.FirstOrDefault(i => i.Id == newItem.Id);
            if (pmcItem is not null)
            {
                pmcItem.AddUpd();
                if (pmcItem.Upd is not null) pmcItem.Upd.SpawnedInSession = isFir;
            }
        }
    }

    private static bool HasMusclePain(BotBaseHealth? health)
    {
        var effects = health?.BodyParts?.GetValueOrDefault("Chest")?.Effects;
        return effects is not null && (effects.ContainsKey("MildMusclePain") || effects.ContainsKey("SevereMusclePain"));
    }

    private static void ReduceMusclePain(BotBaseHealth? health, double factor)
    {
        if (factor <= 0) return;
        var effects = health?.BodyParts?.GetValueOrDefault("Chest")?.Effects;
        if (effects is null) return;

        foreach (var key in new[] { "MildMusclePain", "SevereMusclePain" })
        {
            if (effects.TryGetValue(key, out var effect) && effect is not null)
            {
                if (effect.Time.HasValue)
                {
                    var newTime = Math.Round(effect.Time.Value / factor);
                    if (newTime <= 0)
                    {
                        effects.Remove(key);
                    }
                    else
                    {
                        effect.Time = newTime;
                    }
                }
                else
                {
                    effects.Remove(key);
                }
            }
        }
    }

    private static void SyncHealthChanges(ItemEventRouterResponse response, MongoId sessionId, PmcData pmcData)
    {
        if (response.ProfileChanges?.TryGetValue(sessionId, out var changes) != true || changes is null) return;

        if (changes.Health is not null)
        {
            if (changes.Health.BodyParts is null)
            {
                changes.Health.BodyParts = pmcData.Health?.BodyParts;
            }
            else if (pmcData.Health?.BodyParts?.TryGetValue("Chest", out var pmcChest) == true)
            {
                if (changes.Health.BodyParts.TryGetValue("Chest", out var changeChest))
                {
                    changeChest.Effects = pmcChest.Effects;
                }
                else
                {
                    changes.Health.BodyParts["Chest"] = pmcChest;
                }
            }
        }
        else if (pmcData.Health is not null)
        {
            changes.Health = pmcData.Health;
        }
    }

    private static void ClearMusclePain(BotBaseHealth? health)
    {
        var effects = health?.BodyParts?.GetValueOrDefault("Chest")?.Effects;
        effects?.Remove("MildMusclePain");
        effects?.Remove("SevereMusclePain");
    }
}

