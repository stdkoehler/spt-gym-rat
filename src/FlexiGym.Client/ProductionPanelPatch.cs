using System;
using System.Collections.Generic;
using System.Linq;
using EFT.Hideout;
using HarmonyLib;

namespace FlexiGym.Client;

[HarmonyPatch(typeof(ProductionPanel))]
public static class ProductionPanelPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ProductionPanel.GetSortedSchemes))]
    public static void Postfix_GetSortedSchemes(ref IEnumerable<ProductionScheme> __result)
    {
        try
        {
            if (__result == null) return;

            // Ensure FlexiGym recovery crafts are always prioritized at the top of the craft list
            __result = __result.OrderBy(scheme =>
            {
                bool isFlexiGymCraft = scheme._id == ProduceViewPatch.ShowerRecipeId
                                    || scheme._id == ProduceViewPatch.NutritionRecipeId;
                return isFlexiGymCraft ? 0 : 1;
            });
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"[FlexiGym] Error sorting production schemes: {ex}");
        }
    }
}
