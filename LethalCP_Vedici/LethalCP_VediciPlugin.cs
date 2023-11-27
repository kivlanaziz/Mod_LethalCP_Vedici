using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LethalCP_Vedici
{

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class LethalCP_VediciPlugin : BaseUnityPlugin
    {
        #region mod desc constant
        // Mod specific details. MyGUID should be unique, and follow the reverse domain pattern
        // e.g.
        // com.mynameororg.pluginname
        // Version should be a valid version string.
        // e.g.
        // 1.0.0
        private const string MyGUID = "com.kivlan.LethalCP_Vedici";
        private const string PluginName = "LethalCP_Vedici";
        private const string VersionString = "1.0.0";
        #endregion
        #region config manager
        // Config entry key strings
        // These will appear in the config file created by BepInEx and can also be used
        // by the OnSettingsChange event to determine which setting has changed.
        private static string cfgNightVisionKey = "Night Vision Configuration";
        private static string HideCommandMessagesKey = "Toggle Hide Command Messages";
        private static string CustomTimeScaleKey = "Toggle Custom Time Scale";
        private static string UseRandomTimeScaleKey = "Toggle Random Time Scale";
        private static string MaximumTimeScaleKey = "Maximum Time Scale (Will use this value when random time scale is disabled)";
        private static string MinimumTimeScaleKey = "Minimum Time Scale";

        // Configuration entries. Static, so can be accessed directly elsewhere in code via
        // e.g.
        // float myFloat = LethalCP_VediciPlugin.FloatExample.Value;
        // TODO Change this code or remove the code if not required.
        private static ConfigEntry<bool> cfgNightVision;
        private static ConfigEntry<bool> HideCommandMessages;
        private static ConfigEntry<bool> CustomTimeScale;
        private static ConfigEntry<bool> UseRandomTimeScale;
        private static ConfigEntry<float> MaximumTimeScale;
        private static ConfigEntry<float> MinimumTimeScale;
        #endregion

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        private static PlayerControllerB playerRef;
        private static bool nightVision;
        private static float nightVisionIntensity;
        private static float nightVisionRange;
        private static UnityEngine.Color nightVisionColor;
        private static bool isHost = true;
        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            cfgNightVision = Config.Bind("Player Settings", cfgNightVisionKey, false);
            HideCommandMessages = Config.Bind("UI Settings", HideCommandMessagesKey, false);
            CustomTimeScale = Config.Bind("Time Settings", CustomTimeScaleKey, false);
            UseRandomTimeScale = Config.Bind("Time Settings", UseRandomTimeScaleKey, false);
            MaximumTimeScale = Config.Bind("Time Settings", MaximumTimeScaleKey, 2f);
            MinimumTimeScale = Config.Bind("Time Settings", MinimumTimeScaleKey, 0.5f);

            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll(typeof(LethalCP_VediciPlugin));
            Harmony.PatchAll(typeof(PlayerControllerB));
            Harmony.PatchAll(typeof(HUDManager));
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            cfgNightVision.SettingChanged += nightVisionCFGChanged;
            // Sets up our static Log, so it can be used elsewhere in code.
            // .e.g.
            // LethalCP_VediciPlugin.Log.LogDebug("Debug Message to BepInEx log file");
            Log = Logger;
        }

        /// <summary>
        /// Code executed every frame. See below for an example use case
        /// to detect keypress via custom configuration.
        /// </summary>
        // TODO - Add your code here or remove this section if not required.
        private void Update()
        {
            
        }

        /// <summary>
        /// Method to handle changes to configuration made by the player
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigSettingChanged(object sender, System.EventArgs e)
        {
            SettingChangedEventArgs settingChangedEventArgs = e as SettingChangedEventArgs;

            // Check if null and return
            if (settingChangedEventArgs == null)
            {
                return;
            }
        }

        /// <summary>
        /// Method to handle changes to night vision configuration made from config manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nightVisionCFGChanged(object sender, EventArgs e)
        {
            if (!isHost)
            {
                return;
            }
            nightVision = cfgNightVision.Value;

            if (nightVision)
            {
                LethalCP_VediciPlugin.Log.LogInfo("Night Vision Turned On");
            }
            else
            {
                LethalCP_VediciPlugin.Log.LogInfo("Night Vision Turned Off");
            }
        }

        /// <summary>
        /// Method to get the initial configuration of night vision
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPrefix]
        static void getNightVision(ref PlayerControllerB __instance)
        {
            playerRef = __instance;
            nightVision = playerRef.nightVision.enabled;
            // store nightvision values
            nightVisionIntensity = playerRef.nightVision.intensity;
            nightVisionColor = playerRef.nightVision.color;
            nightVisionRange = playerRef.nightVision.range;

            playerRef.nightVision.color = UnityEngine.Color.green;
            playerRef.nightVision.intensity = 1000f;
            playerRef.nightVision.range = 10000f;
        }

        /// <summary>
        /// Method to update the night vision config based on the night vision status. (Updated per frame)
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "SetNightVisionEnabled")]
        [HarmonyPostfix]
        static void updateNightVision()
        {
            //instead of enabling/disabling nightvision, set the variables
            
            if (nightVision)
            {
                playerRef.nightVision.color = UnityEngine.Color.green;
                playerRef.nightVision.intensity = 1000f;
                playerRef.nightVision.range = 10000f;
            }
            else
            {
                playerRef.nightVision.color = nightVisionColor;
                playerRef.nightVision.intensity = nightVisionIntensity;
                playerRef.nightVision.range = nightVisionRange;
            }
            // should always be on
            playerRef.nightVision.enabled = true;
        }

        /// <summary>
        /// Method to update time scale using custom modifier
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(TimeOfDay), "MoveGlobalTime")]
        [HarmonyPostfix]
        static void customizableTimeScale(TimeOfDay __instance)
        {
            if (isHost)
            {
                if (CustomTimeScale.Value)
                {
                    __instance.globalTimeSpeedMultiplier = MaximumTimeScale.Value;

                    if (UseRandomTimeScale.Value)
                    {
                        __instance.globalTimeSpeedMultiplier = Random.Range(MinimumTimeScale.Value, MaximumTimeScale.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Display text box on the start of the game
        /// </summary>
        [HarmonyPatch(typeof(StartOfRound), "openingDoorsSequence")]
        [HarmonyPostfix]
        static void customOpeningScreen()
        {
            if (isHost)
            {
                string noticeTitle = "Modded Game";
                string noticeBody = "Made with <3 by Stoichev";
                HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);
            }
        }
        //[HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
        //[HarmonyPrefix]
        //static void chatCommand(HUDManager __instance)
        //{
        //    string text = __instance.chatTextField.text;
        //    // make prefix even if one doesn't exist
        //    string tempPrefix = "/";
        //    mls.LogInfo(text);

        //    // check if prefix is utilized
        //    if (text.ToLower().StartsWith(tempPrefix.ToLower()))
        //    {
        //        string noticeTitle = "Default Title";
        //        string noticeBody = "Default Body";

        //        if (!isHost)
        //        {
        //            noticeTitle = "Command";
        //            noticeBody = "Unable to send command since you are not host.";
        //            HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);
        //            if (HideCommandMessages.Value)
        //            {
        //                __instance.chatTextField.text = "";
        //            }
        //            return;
        //        }
        //        if (text.ToLower().Contains("night") || text.ToLower().Contains("vision"))
        //        {
        //            if (toggleNightVision())
        //            {
        //                noticeBody = "Enabled Night Vision";
        //            }
        //            else
        //            {
        //                noticeBody = "Disabled Night Vision";
        //            }
        //            noticeTitle = "Night Vision";
        //        }
        //        // sends notice to user about what they have done
        //        HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);

        //        // ensures value is hidden if set but path doesn't hide it
        //        if (HideCommandMessages.Value)
        //        {
        //            __instance.chatTextField.text = "";
        //        }
        //        return;
        //    }
        //}

        /// <summary>
        /// Method to toggle night vision status
        /// </summary>
        private static bool toggleNightVision()
        {

            if (isHost)
            {
                nightVision = !nightVision;
                cfgNightVision.Value = nightVision;
            }

            if (nightVision)
            {
                LethalCP_VediciPlugin.Log.LogInfo("Night Vision Turned On");
            }
            else
            {
                LethalCP_VediciPlugin.Log.LogInfo("Night Vision Turned Off");
            }

            return nightVision;
        }
    }
}
