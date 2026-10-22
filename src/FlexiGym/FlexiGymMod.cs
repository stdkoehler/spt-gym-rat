using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.DI.Routing;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Request;
using SPTarkov.Server.Core.Models.Eft.Health;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers.ItemEvents;
using SPTarkov.Server.Core.Utils;
using BotBaseHealth = SPTarkov.Server.Core.Models.Eft.Common.Tables.BotBaseHealth;

namespace FlexiGym;

/// <summary>
/// The gym "reset" is the muscle pain applied when a workout finishes; its duration comes from
/// the finishEffect of every entry in hideout/qte.json (vanilla: 86400s).
///  1. Rewriting that duration once at load makes server and client use the configured time.
///  2. Consuming a configured item clears the muscle pain at once. The server's "Eat" route is
///     wrapped for this (see HookEatRoute); if that fails the mod keeps working without it.
/// A broken config disables the mod instead of crashing the server.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad)]
public class FlexiGymMod(
    ISptLogger<FlexiGymMod> logger,
    ModHelper modHelper,
    HideoutTable hideoutTable,
    ItemEventCallbacks itemEventCallbacks) : IOnLoad
{
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

    private static void SyncHealthChanges(ItemEventRouterResponse response, MongoId sessionId, PmcData pmcData)
    {
        if (response.ProfileChanges?.TryGetValue(sessionId, out var changes) != true || changes is null) return;

        if (changes.Health is not null)
        {
            ClearMusclePain(changes.Health);
            changes.Health.BodyParts ??= pmcData.Health?.BodyParts;
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
