using System;
using System.Threading.Tasks;
using EFT.Hideout;
using HarmonyLib;

namespace GymRat.Client;

[HarmonyPatch(typeof(HideoutRepresentation))]
public static class HideoutRepresentationPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(HideoutRepresentation.GetProducedItems))]
    public static void Postfix_GetProducedItems(string schemeId, Task<bool> __result)
    {
        try
        {
            if (string.IsNullOrEmpty(schemeId) || __result == null) return;
            if (schemeId != ProduceViewPatch.ShowerRecipeId && schemeId != ProduceViewPatch.NutritionRecipeId) return;

            __result.ContinueWith(task =>
            {
                if (task.IsCompleted && task.Result)
                {
                    Plugin.QueueOnMainThread(() =>
                    {
                        RecoveryHelper.HandleCraftTaken(schemeId, Plugin.NutritionReductionFactor.Value);
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"[GymRat] Error in HideoutRepresentation.GetProducedItems patch: {ex}");
        }
    }
}
