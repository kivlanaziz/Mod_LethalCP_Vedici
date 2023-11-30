using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using DunGen;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Steamworks;
using Steamworks.Data;
using System.Collections;
using System.Security.AccessControl;
using GameNetcodeStuff;
using BepInEx.Configuration;
using System.Reflection;
using Unity.Netcode;
using static System.Net.Mime.MediaTypeNames;
using Steamworks.Ugc;
using System.Xml.Schema;

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
        private const string VersionString = "1.0.8";
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
        private static string NightVisionIntensityKey = "Brightness level of night vision";
        private static string NightVisionRangeKey = "Range of night vision";
        private static string SpeedMultiplierKey = "Sprint Speed Multiplier";
        private static string MaxStaminaMultiplierKey = "Max Stamina Multiplier";
        private static string StaminaRegenMultiplierKey = "Stamina Regen Multiplier";
        private static string WeightStaminaMultiplierKey = "Weight Stamina Multiplier";
        private static string CustomScrapValueMultiplierKey = "Scrap Value Multiplier";
        private static string CustomScrapAmountMultiplierKey = "Scrap Amount Multiplier";
        private static string CustomMapSizeMultiplierKey = "Map Size Multiplier";
        private static string UseCustomMapSettingsKey = "Toggle Custom Map Settings";
        private static string EnableCustomDeadlineKey = "Toggle Custom Deadline";

        // Configuration entries. Static, so can be accessed directly elsewhere in code via
        // e.g.
        // float myFloat = LethalCP_VediciPlugin.FloatExample.Value;
        // TODO Change this code or remove the code if not required.
        public static ConfigEntry<bool> cfgNightVision;
        public static ConfigEntry<bool> HideCommandMessages;
        public static ConfigEntry<bool> CustomTimeScale;
        public static ConfigEntry<bool> UseRandomTimeScale;
        public static ConfigEntry<float> MaximumTimeScale;
        public static ConfigEntry<float> MinimumTimeScale;
        public static ConfigEntry<float> NightVisionIntensity;
        public static ConfigEntry<float> NightVisionRange;
        public static ConfigEntry<float> SpeedMultiplier;
        public static ConfigEntry<float> MaxStaminaMultiplier;
        public static ConfigEntry<float> StaminaRegenMultiplier;
        public static ConfigEntry<float> WeightStaminaMultiplier;
        public static ConfigEntry<float> CustomScrapValueMultiplier;
        public static ConfigEntry<float> CustomScrapAmountMultiplier;
        public static ConfigEntry<float> CustomMapSizeMultiplier;
        public static ConfigEntry<bool> UseCustomMapSettings;
        public static ConfigEntry<bool> EnableCustomDeadline;
        #endregion

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static PlayerControllerB playerRef;
        public static bool nightVision;
        public static float defaultNightVisionIntensity;
        public static float defaultNightVisionRange;
        public static UnityEngine.Color nightVisionColor;
        public static bool isHost = true;
        public static float currentStaminaMeter;
        public static float currentWeight;
        public static SelectableLevel currentLevel;
        public static bool resetCustomMultiplier = false;
        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            cfgNightVision = Config.Bind("Player Settings", cfgNightVisionKey, false);
            HideCommandMessages = Config.Bind("UI Settings", HideCommandMessagesKey, true);
            CustomTimeScale = Config.Bind("Time Settings", CustomTimeScaleKey, false);
            UseRandomTimeScale = Config.Bind("Time Settings", UseRandomTimeScaleKey, false);
            MaximumTimeScale = Config.Bind("Time Settings", MaximumTimeScaleKey, 2f);
            MinimumTimeScale = Config.Bind("Time Settings", MinimumTimeScaleKey, 0.5f);
            NightVisionIntensity = Config.Bind("Player Settings", NightVisionIntensityKey, 1000f);
            NightVisionRange = Config.Bind("Player Settings", NightVisionRangeKey, 10000f);
            SpeedMultiplier = Config.Bind("Player Settings", SpeedMultiplierKey, 1f);
            MaxStaminaMultiplier = Config.Bind("Player Settings", MaxStaminaMultiplierKey, 1f);
            StaminaRegenMultiplier = Config.Bind("Player Settings", StaminaRegenMultiplierKey, 1.5f);
            WeightStaminaMultiplier = Config.Bind("Player Settings", WeightStaminaMultiplierKey, 0.75f);
            UseCustomMapSettings = Config.Bind("Map Settings", UseCustomMapSettingsKey, false);
            CustomScrapAmountMultiplier = Config.Bind("Map Settings", CustomScrapAmountMultiplierKey, 1f);
            CustomScrapValueMultiplier = Config.Bind("Map Settings", CustomScrapValueMultiplierKey, 1f);
            CustomMapSizeMultiplier = Config.Bind("Map Settings", CustomMapSizeMultiplierKey, 1f, "Be careful when modifying this value since it will make the game laggy");
            EnableCustomDeadline = Config.Bind("Game Settings", EnableCustomDeadlineKey, false);

            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
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

        #region internal method
        /// <summary>
        /// Method to toggle night vision status
        /// </summary>
        public static bool toggleNightVision()
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
        #endregion
    }
}
