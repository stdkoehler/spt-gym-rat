using System;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.NetworkPackets;
using HarmonyLib;

namespace GymRat.Client;

[HarmonyPatch(typeof(OfflineHealthController))]
public static class OfflineHealthControllerPatch
{
    public static OfflineHealthController? ActiveInstance { get; internal set; }

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor, typeof(Profile.HealthInfo), typeof(InventoryController), typeof(SkillManager), typeof(bool))]
    public static void Postfix_Ctor(OfflineHealthController __instance)
    {
        ActiveInstance = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(OfflineHealthController.ApplyItem), typeof(Item), typeof(OneAndList<EBodyPart>), typeof(float?))]
    public static void Postfix_ApplyItem(OfflineHealthController __instance, Item item, bool __result)
    {
        try
        {
            if (!__result || item == null) return;
            ActiveInstance = __instance;

            if (Plugin.IsResetItem(item))
            {
                RecoveryHelper.ClearMusclePain();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"[GymRat] Error in ApplyItem patch: {ex}");
        }
    }
}
