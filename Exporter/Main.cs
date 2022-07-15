using BepInEx;
using HarmonyLib;

namespace Exporter;

[BepInAutoPlugin("dev.thebox1.zeepkistexporter")]
[BepInProcess("Zeepkist.exe")]
public partial class Main : BaseUnityPlugin
{
    public new static BepInEx.Logging.ManualLogSource Logger;

    private Harmony Harmony { get; } = new (Id);
    
    public static bool garage = false;

    public void Awake()
    {
        Logger = base.Logger;
        Harmony.PatchAll();
        Logger.LogMessage("Loaded Exporter");
    }
}