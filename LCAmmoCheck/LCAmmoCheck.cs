using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Assertions;

namespace LCAmmoCheck
{
    [BepInPlugin(GeneratedPluginInfo.Identifier, GeneratedPluginInfo.Name, GeneratedPluginInfo.Version)]
    public class LCAmmoCheckPlugin : BaseUnityPlugin
    {
        private static Harmony? harmony;
        public static LCAmmoCheckPlugin? Instance { get; private set; }
        public static AnimationClip? ShotgunInspectClip { get; private set; }
        public static AudioClip? ShotgunInspectSFX { get; private set; }

        private static void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LCAmmoCheck.lcammocheck");
            AssetBundle ACAssetBundle = AssetBundle.LoadFromStream(stream);
            ShotgunInspectClip = ACAssetBundle.LoadAsset<AnimationClip>("Assets/AnimationClip/ShotgunInspect.anim");
            Assert.IsNotNull(ShotgunInspectClip);
            ShotgunInspectSFX = ACAssetBundle.LoadAsset<AudioClip>("Assets/AudioClip/ShotgunInspect.ogg");
            Assert.IsNotNull(ShotgunInspectSFX);
            ShotgunInspectSFX?.LoadAudioData();
            ACAssetBundle.Unload(false);
        }


        public void Awake()
        {
            Instance = this;
            LoadAssetBundle();
            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GeneratedPluginInfo.Identifier);
            Logger.Log(LogLevel.Message, "LCAmmoCheck loaded!");
        }

        public static void OnDestroy()
        {
            harmony?.UnpatchSelf();
            Instance = null;
            harmony = null;
            Debug.Log("LCAmmoCheck unloaded!");
        }
    }
}
