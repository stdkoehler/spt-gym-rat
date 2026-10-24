using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace GymRat.Client;

[BepInPlugin("com.spt.gymrat", "GymRat", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        new Harmony("com.spt.gymrat").PatchAll();
        Log.LogInfo("GymRat client plugin initialized.");
    }
}
