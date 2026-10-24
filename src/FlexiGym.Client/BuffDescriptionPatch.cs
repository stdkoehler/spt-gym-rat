using System;
using EFT.HealthSystem;
using HarmonyLib;

namespace FlexiGym.Client;

[HarmonyPatch(typeof(BuffDescription))]
public static class BuffDescriptionPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(BuffDescription.TimeLeftApproximatelyDisplay), MethodType.Getter)]
    public static bool Prefix_TimeLeftApproximatelyDisplay(BuffDescription __instance, ref string __result)
    {
        try
        {
            if (IsMusclePain(__instance))
            {
                __result = FormatMusclePainBuff(__instance);
                return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"[FlexiGym] Error in BuffDescription.TimeLeftApproximatelyDisplay prefix: {ex}");
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(BuffDescription.TimeLeftDisplay), MethodType.Getter)]
    public static bool Prefix_TimeLeftDisplay(BuffDescription __instance, ref string __result)
    {
        try
        {
            if (IsMusclePain(__instance))
            {
                __result = FormatMusclePainBuff(__instance);
                return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"[FlexiGym] Error in BuffDescription.TimeLeftDisplay prefix: {ex}");
        }
        return true;
    }

    public static bool IsMusclePain(BuffDescription? buff)
    {
        if (buff == null) return false;
        return !string.IsNullOrEmpty(buff.Text) &&
               buff.Text.IndexOf("MusclePain", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string FormatMusclePainBuff(BuffDescription buff)
    {
        string name = buff.NameDisplay ?? string.Empty;
        int timeLeft = buff.TimeLeft;
        if (timeLeft <= 0)
        {
            return name;
        }

        string formattedTime = TimeFormatUtil.FormatTimeLeft(timeLeft);
        if (string.IsNullOrEmpty(formattedTime))
        {
            return name;
        }

        return string.IsNullOrEmpty(name) ? $"({formattedTime})" : $"{name}\n({formattedTime})";
    }
}
