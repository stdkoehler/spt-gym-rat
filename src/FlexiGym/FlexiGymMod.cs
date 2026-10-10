using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace FlexiGym;

/// <summary>
/// The gym "reset" is the muscle pain applied when a workout finishes; its duration comes from
/// the finishEffect of every entry in hideout/qte.json (vanilla: 86400s). Rewriting that duration
/// once at load makes both the server (HideoutController.HandleMusclePain) and the client use it.
/// A broken config disables the mod instead of crashing the server.
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad)]
public class FlexiGymMod(
    ISptLogger<FlexiGymMod> logger,
    ModHelper modHelper,
    HideoutTable hideoutTable) : IOnLoad
{
    private const string Tag = "[FlexiGym]";

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

        var seconds = config.GymResetMinutes * 60L;
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

        logger.Success($"{Tag} Gym resets every {config.GymResetMinutes} min ({patched} workouts patched)");
        return Task.CompletedTask;
    }
}
