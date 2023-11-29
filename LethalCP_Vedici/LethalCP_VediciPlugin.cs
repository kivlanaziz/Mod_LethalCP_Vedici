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
        private const string VersionString = "1.0.7";
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
        private static ConfigEntry<bool> cfgNightVision;
        private static ConfigEntry<bool> HideCommandMessages;
        private static ConfigEntry<bool> CustomTimeScale;
        private static ConfigEntry<bool> UseRandomTimeScale;
        private static ConfigEntry<float> MaximumTimeScale;
        private static ConfigEntry<float> MinimumTimeScale;
        private static ConfigEntry<float> NightVisionIntensity;
        private static ConfigEntry<float> NightVisionRange;
        private static ConfigEntry<float> SpeedMultiplier;
        private static ConfigEntry<float> MaxStaminaMultiplier;
        private static ConfigEntry<float> StaminaRegenMultiplier;
        private static ConfigEntry<float> WeightStaminaMultiplier;
        private static ConfigEntry<float> CustomScrapValueMultiplier;
        private static ConfigEntry<float> CustomScrapAmountMultiplier;
        private static ConfigEntry<float> CustomMapSizeMultiplier;
        private static ConfigEntry<bool> UseCustomMapSettings;
        private static ConfigEntry<bool> EnableCustomDeadline;
        #endregion

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        private static PlayerControllerB playerRef;
        private static bool nightVision;
        private static float defaultNightVisionIntensity;
        private static float defaultNightVisionRange;
        private static UnityEngine.Color nightVisionColor;
        private static bool isHost = true;
        private static float currentStaminaMeter;
        private static float currentWeight;
        private static SelectableLevel currentLevel;
        private static bool resetCustomMultiplier = false;
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
            defaultNightVisionIntensity = playerRef.nightVision.intensity;
            nightVisionColor = playerRef.nightVision.color;
            defaultNightVisionRange = playerRef.nightVision.range;
            if (cfgNightVision.Value)
            {
                playerRef.nightVision.color = UnityEngine.Color.green;
                playerRef.nightVision.intensity = 1000f;
                playerRef.nightVision.range = 10000f;
            }
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
                playerRef.nightVision.intensity = NightVisionIntensity.Value;
                playerRef.nightVision.range = NightVisionRange.Value;
            }
            else
            {
                playerRef.nightVision.color = nightVisionColor;
                playerRef.nightVision.intensity = defaultNightVisionIntensity;
                playerRef.nightVision.range = defaultNightVisionRange;
            }

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

        /// <summary>
        /// Patch Stamina Multiplier
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPrefix]
        static void updatePlayerMovementPrefix(PlayerControllerB __instance)
        {
            //__instance.movementSpeed *= SpeedMultiplier.Value;
            if (__instance.isPlayerControlled)
            {
                currentStaminaMeter = __instance.sprintMeter;
                currentWeight = __instance.carryWeight;
                __instance.carryWeight = Mathf.Max(__instance.carryWeight * WeightStaminaMultiplier.Value, 1f);
            }
        }

        /// <summary>
        /// Patch Stamina Multiplier
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void updatePlayerMovementPostfix(PlayerControllerB __instance)
        {
            //__instance.movementSpeed *= SpeedMultiplier.Value;
            if (__instance.isPlayerControlled)
            {
                float x = __instance.sprintMeter - currentStaminaMeter;
                if (x < 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Sprint Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Max(__instance.sprintMeter + x / MaxStaminaMultiplier.Value, 0f);
                }
                else if (x > 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Walk Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Min(__instance.sprintMeter + x * StaminaRegenMultiplier.Value, 1f);
                }
                __instance.carryWeight = currentWeight;
            }
        }

        /// <summary>
        /// Patch Stamina Multiplier
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPrefix]
        static void lateUpdatePlayerMovementPrefix(PlayerControllerB __instance)
        {
            //__instance.movementSpeed *= SpeedMultiplier.Value;
            if (__instance.isPlayerControlled)
            {
                currentStaminaMeter = __instance.sprintMeter;
                currentWeight = __instance.carryWeight;
                __instance.carryWeight = Mathf.Max(__instance.carryWeight * WeightStaminaMultiplier.Value, 1f);
            }
        }

        /// <summary>
        /// Patch Stamina Multiplier
        /// </summary>
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        static void lateUpdatePlayerMovementPostfix(PlayerControllerB __instance)
        {
            //__instance.movementSpeed *= SpeedMultiplier.Value;
            if (__instance.isPlayerControlled)
            {
                float x = __instance.sprintMeter - currentStaminaMeter;
                if (x < 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Sprint Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Max(__instance.sprintMeter + x / MaxStaminaMultiplier.Value, 0f);
                }
                else if (x > 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Walk Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Min(__instance.sprintMeter + x * StaminaRegenMultiplier.Value, 1f);
                }
                __instance.carryWeight = currentWeight;
            }
        }

        [HarmonyPatch(typeof(RoundManager), "LoadNewLevel")]
        [HarmonyPrefix]
        static void LoadNewLevelPrefixPatch(RoundManager __instance, SelectableLevel newLevel)
        {
            currentLevel = newLevel;
            if (UseCustomMapSettings.Value)
            {
                if (resetCustomMultiplier)
                {
                    LethalCP_VediciPlugin.Log.LogInfo("Resetting Custom Multiplier Value");
                    __instance.scrapAmountMultiplier /= CustomScrapAmountMultiplier.Value;
                    __instance.scrapValueMultiplier /= CustomScrapValueMultiplier.Value;
                    __instance.mapSizeMultiplier /= CustomMapSizeMultiplier.Value;
                }

                __instance.scrapAmountMultiplier *= CustomScrapAmountMultiplier.Value;
                __instance.scrapValueMultiplier *= CustomScrapValueMultiplier.Value;
                __instance.mapSizeMultiplier *= CustomMapSizeMultiplier.Value;
                resetCustomMultiplier = true;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.MoveGlobalTime))]
        [HarmonyPrefix]
        static void InfiniteDeadline(ref float ___timeUntilDeadline)
        {
            if (!isHost) { return; }
            if (EnableCustomDeadline.Value) 
            {
                ___timeUntilDeadline = 999;
            }

        }
        /// <summary>
        /// Method to listen to commands from text chat (require host)
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
        [HarmonyPrefix]
        static void chatCommand(HUDManager __instance)
        {
            string text = __instance.chatTextField.text;
            // make prefix even if one doesn't exist
            string tempPrefix = "/";
            LethalCP_VediciPlugin.Log.LogInfo(text);

            // check if prefix is utilized
            if (text.ToLower().StartsWith(tempPrefix.ToLower()))
            {
                string noticeTitle = "Default Title";
                string noticeBody = "Default Body";

                if (!isHost)
                {
                    noticeTitle = "Command";
                    noticeBody = "Unable to send command since you are not host.";
                    HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);
                    if (HideCommandMessages.Value)
                    {
                        __instance.chatTextField.text = "";
                    }
                    return;
                }
                if (text.ToLower().Contains("night") || text.ToLower().Contains("vision"))
                {
                    if (toggleNightVision())
                    {
                        noticeBody = "Enabled Night Vision";
                    }
                    else
                    {
                        noticeBody = "Disabled Night Vision";
                    }
                    noticeTitle = "Night Vision";
                }
                if (text.ToLower().Contains("scan"))
                {
                    int totalItems = 0, totalValue = 0;
                    findItemsOutsideShip(out totalItems, out totalValue);
                    noticeTitle = "Scan Result";
                    noticeBody = $"There are {totalItems} objects outside the ship, totalling at an approximate value of ${totalValue}.";
                }
                if (text.ToLower().Contains("player"))
                {
                    int totalPlayerAlive = 0, totalPlayerDead = 0;
                    findTeamStatus(out totalPlayerAlive, out totalPlayerDead);
                    noticeTitle = "Scan Result";
                    noticeBody = $"There are {totalPlayerAlive} Player Alive and {totalPlayerDead} Player(s) Dead or Disconnected.";
                }
                if (text.ToLower().Contains("time"))
                {
                    string currentTime = HUDManager.Instance.SetClock(TimeOfDay.Instance.normalizedTimeOfDay, TimeOfDay.Instance.numberOfHours, false);
                    noticeTitle = "Scan Result";
                    noticeBody = $"Time at: {currentTime}";
                }
                if (text.ToLower().Contains("countenemy"))
                {
                    int approximateEnemy = UnityEngine.Object.FindObjectsOfType<EnemyAI>().Count();
                    noticeTitle = "Scan Result";
                    noticeBody = $"There are approximately {approximateEnemy} enemy detected! (includes docile, turret & landmine)";
                }
                // sends notice to user about what they have done
                HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);

                // ensures value is hidden if set but path doesn't hide it
                if (HideCommandMessages.Value)
                {
                    __instance.chatTextField.text = "";
                }
                return;
            }
        }

        #region internal method
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

        /// <summary>
        /// Method to find grabable items outside ship
        /// </summary>
        /// <param name="totalItems"></param>
        /// <param name="totalValue"></param>
        private static void findItemsOutsideShip(out int totalItems, out int totalValue)
        {
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 91);
            totalValue = 0;
            totalItems = 0;
            int num4 = 0;
            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            for (int num5 = 0; num5 < array.Length; num5++)
            {
                if (array[num5].itemProperties.isScrap && !array[num5].isInShipRoom && !array[num5].isInElevator)
                {
                    num4 += array[num5].itemProperties.maxValue - array[num5].itemProperties.minValue;
                    totalValue += Mathf.Clamp(random.Next(array[num5].itemProperties.minValue, array[num5].itemProperties.maxValue), array[num5].scrapValue - 6 * num5, array[num5].scrapValue + 9 * num5);
                    totalItems++;
                }
            }
        }

        /// <summary>
        /// Method to find player status
        /// </summary>
        /// <param name="totalPlayerAlive"></param>
        /// <param name="totalPlayerDead"></param>
        private static void findTeamStatus(out int totalPlayerAlive, out int totalPlayerDead)
        {
            totalPlayerAlive = 0;
            totalPlayerDead = 0;
            PlayerControllerB[] array = UnityEngine.Object.FindObjectsOfType<PlayerControllerB>();
            for (int num1 = 0; num1 < array.Length; num1++)
            {
                if ((array[num1].isPlayerDead || array[num1].disconnectedMidGame) && array[num1].isPlayerControlled)
                {
                    totalPlayerDead++;
                }
                else if (array[num1].isPlayerControlled)
                {
                    totalPlayerAlive++;
                }
            }
        }
        #endregion
    }
}
