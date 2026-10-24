using System;
using EFT.HealthSystem;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace GymRat.Client;

public static class RecoveryHelper
{
    private static string? _lastProcessedScheme;
    private static float _lastProcessedTime;

    public static OfflineHealthController? GetHealthController()
    {
        if (OfflineHealthControllerPatch.ActiveInstance != null)
        {
            return OfflineHealthControllerPatch.ActiveInstance;
        }

        if (ItemUiContext.Instance != null)
        {
            var field = AccessTools.Field(typeof(ItemUiContext), "_healthController");
            if (field != null)
            {
                var hc = field.GetValue(ItemUiContext.Instance) as OfflineHealthController;
                if (hc != null) return hc;
            }
        }

        return null;
    }

    public static void HandleCraftTaken(string schemeId, float factor)
    {
        if (string.IsNullOrEmpty(schemeId)) return;
        if (schemeId != ProduceViewPatch.ShowerRecipeId && schemeId != ProduceViewPatch.NutritionRecipeId) return;

        // Debounce to prevent duplicate executions if both HideoutRepresentation and ProductionPanel fire
        if (_lastProcessedScheme == schemeId && (Time.time - _lastProcessedTime) < 2f)
        {
            return;
        }
        _lastProcessedScheme = schemeId;
        _lastProcessedTime = Time.time;

        if (schemeId == ProduceViewPatch.ShowerRecipeId)
        {
            ClearMusclePain();
        }
        else if (schemeId == ProduceViewPatch.NutritionRecipeId)
        {
            ReduceMusclePain(factor);
        }
    }

    public static void ClearMusclePain()
    {
        var hc = GetHealthController();
        if (hc == null)
        {
            Plugin.Log?.LogWarning("[GymRat] Could not find OfflineHealthController to clear muscle pain.");
            return;
        }

        var mild = hc.FindActiveEffect<OfflineHealthController.MildMusclePain>();
        var severe = hc.FindActiveEffect<OfflineHealthController.SevereMusclePain>();

        if (mild != null || severe != null)
        {
            Plugin.Log?.LogInfo("[GymRat] Clearing muscle pain on client OfflineHealthController.");
            mild?.Remove();
            severe?.Remove();
        }
    }

    public static void ReduceMusclePain(float factor)
    {
        if (factor <= 0f) return;
        var hc = GetHealthController();
        if (hc == null)
        {
            Plugin.Log?.LogWarning("[GymRat] Could not find OfflineHealthController to reduce muscle pain.");
            return;
        }

        var mild = hc.FindActiveEffect<OfflineHealthController.MildMusclePain>();
        var severe = hc.FindActiveEffect<OfflineHealthController.SevereMusclePain>();

        if (mild != null)
        {
            float remaining = mild.TimeLeft;
            float newTime = Mathf.Round(remaining / factor);
            Plugin.Log?.LogInfo($"[GymRat] Reducing MildMusclePain on client: {remaining:0}s -> {newTime:0}s (factor {factor:0.#})");
            mild.Remove();
            if (newTime > 0f)
            {
                hc.CreateMusclePain<OfflineHealthController.MildMusclePain>(newTime);
            }
        }
        else if (severe != null)
        {
            float remaining = severe.TimeLeft;
            float newTime = Mathf.Round(remaining / factor);
            Plugin.Log?.LogInfo($"[GymRat] Reducing SevereMusclePain on client: {remaining:0}s -> {newTime:0}s (factor {factor:0.#})");
            severe.Remove();
            if (newTime > 0f)
            {
                hc.CreateMusclePain<OfflineHealthController.SevereMusclePain>(newTime);
            }
        }
    }
}
