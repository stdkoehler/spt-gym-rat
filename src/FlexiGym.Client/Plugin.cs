using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace FlexiGym.Client;

[BepInPlugin("com.spt.flexigym", "FlexiGym", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        new Harmony("com.spt.flexigym").PatchAll();
        Log.LogInfo("FlexiGym client plugin initialized.");
    }
}
