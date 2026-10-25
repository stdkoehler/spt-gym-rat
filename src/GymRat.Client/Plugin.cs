using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.InventoryLogic;
using HarmonyLib;

namespace GymRat.Client;

[BepInPlugin("com.spt.gymrat", "GymRat", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null!;
    public static Plugin Instance { get; private set; } = null!;

    public static ConfigEntry<float> NutritionReductionFactor { get; private set; } = null!;
    public static ConfigEntry<string> ResetItemsCsv { get; private set; } = null!;
    public static ConfigEntry<bool> RequireFoundInRaid { get; private set; } = null!;

    private static readonly Queue<Action> _mainThreadQueue = new();

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        NutritionReductionFactor = Config.Bind("Crafts", "NutritionReductionFactor", 6.0f,
            "Factor by which Nutrition Regime reduces muscle pain timer.");
        ResetItemsCsv = Config.Bind("ItemConsumeReset", "Items", "",
            "Comma-separated template IDs of consumable food/drink items that reset gym muscle pain when consumed in hideout (e.g. 5e8f3423fd7471236e6e3b64 for kvass).");
        RequireFoundInRaid = Config.Bind("ItemConsumeReset", "RequireFoundInRaid", true,
            "Only Found In Raid items trigger gym reset when consumed.");

        new Harmony("com.spt.gymrat").PatchAll();
        Log.LogInfo("GymRat client plugin initialized.");
    }

    public static void QueueOnMainThread(Action action)
    {
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        while (_mainThreadQueue.Count > 0)
        {
            Action? action = null;
            lock (_mainThreadQueue)
            {
                if (_mainThreadQueue.Count > 0) action = _mainThreadQueue.Dequeue();
            }
            action?.Invoke();
        }
    }

    public static bool IsResetItem(Item? item)
    {
        if (item == null) return false;
        if (RequireFoundInRaid.Value && !item.SpawnedInSession) return false;

        string templateId = item.TemplateId.ToString();
        var items = ResetItemsCsv.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var id in items)
        {
            if (string.Equals(id.Trim(), templateId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

