using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LCAmmoCheck
{
    [BepInPlugin(GeneratedPluginInfo.Identifier, GeneratedPluginInfo.Name, GeneratedPluginInfo.Version)]
    public class LCAmmoCheckPlugin : BaseUnityPlugin
    {
        public static LCAmmoCheckPlugin? Instance { get; private set; }
        private static Harmony? harmony;

#pragma warning disable IDE0051
        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GeneratedPluginInfo.Identifier);
            Logger.Log(LogLevel.Message, "LCAmmoCheck loaded!");
        }

        static private void OnDestroy()
        {
            Instance = null;
            harmony?.UnpatchSelf();
        }
#pragma warning restore IDE0051
    }
}
