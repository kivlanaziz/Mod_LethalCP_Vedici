using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalCP_Vedici.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerPatches
    {
        /// <summary>
        /// Method to get the initial configuration of night vision
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPrefix]
        static void getNightVision(ref PlayerControllerB __instance)
        {
            LethalCP_VediciPlugin.playerRef = __instance;
            LethalCP_VediciPlugin.nightVision = LethalCP_VediciPlugin.playerRef.nightVision.enabled;
            // store nightvision values
            LethalCP_VediciPlugin.defaultNightVisionIntensity = LethalCP_VediciPlugin.playerRef.nightVision.intensity;
            LethalCP_VediciPlugin.nightVisionColor = LethalCP_VediciPlugin.playerRef.nightVision.color;
            LethalCP_VediciPlugin.defaultNightVisionRange = LethalCP_VediciPlugin.playerRef.nightVision.range;
            if (LethalCP_VediciPlugin.cfgNightVision.Value)
            {
                LethalCP_VediciPlugin.playerRef.nightVision.color = UnityEngine.Color.green;
                LethalCP_VediciPlugin.playerRef.nightVision.intensity = 1000f;
                LethalCP_VediciPlugin.playerRef.nightVision.range = 10000f;
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

            if (LethalCP_VediciPlugin.nightVision)
            {
                LethalCP_VediciPlugin.playerRef.nightVision.color = UnityEngine.Color.green;
                LethalCP_VediciPlugin.playerRef.nightVision.intensity = LethalCP_VediciPlugin.NightVisionIntensity.Value;
                LethalCP_VediciPlugin.playerRef.nightVision.range = LethalCP_VediciPlugin.NightVisionRange.Value;
            }
            else
            {
                LethalCP_VediciPlugin.playerRef.nightVision.color = LethalCP_VediciPlugin.nightVisionColor;
                LethalCP_VediciPlugin.playerRef.nightVision.intensity = LethalCP_VediciPlugin.defaultNightVisionIntensity;
                LethalCP_VediciPlugin.playerRef.nightVision.range = LethalCP_VediciPlugin.defaultNightVisionRange;
            }

            LethalCP_VediciPlugin.playerRef.nightVision.enabled = true;
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
                LethalCP_VediciPlugin.currentStaminaMeter = __instance.sprintMeter;
                LethalCP_VediciPlugin.currentWeight = __instance.carryWeight;
                __instance.carryWeight = Mathf.Max(__instance.carryWeight * LethalCP_VediciPlugin.WeightStaminaMultiplier.Value, 1f);
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
                float x = __instance.sprintMeter - LethalCP_VediciPlugin.currentStaminaMeter;
                if (x < 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Sprint Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Max(__instance.sprintMeter + x / LethalCP_VediciPlugin.MaxStaminaMultiplier.Value, 0f);
                }
                else if (x > 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Walk Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Min(__instance.sprintMeter + x * LethalCP_VediciPlugin.StaminaRegenMultiplier.Value, 1f);
                }
                __instance.carryWeight = LethalCP_VediciPlugin.currentWeight;
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
                LethalCP_VediciPlugin.currentStaminaMeter = __instance.sprintMeter;
                LethalCP_VediciPlugin.currentWeight = __instance.carryWeight;
                __instance.carryWeight = Mathf.Max(__instance.carryWeight * LethalCP_VediciPlugin.WeightStaminaMultiplier.Value, 1f);
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
                float x = __instance.sprintMeter - LethalCP_VediciPlugin.currentStaminaMeter;
                if (x < 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Sprint Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Max(__instance.sprintMeter + x / LethalCP_VediciPlugin.MaxStaminaMultiplier.Value, 0f);
                }
                else if (x > 0f)
                {
                    //LethalCP_VediciPlugin.Log.LogInfo($"Walk Detected x: {x}. Meter: {__instance.sprintMeter}");
                    __instance.sprintMeter = Mathf.Min(__instance.sprintMeter + x * LethalCP_VediciPlugin.StaminaRegenMultiplier.Value, 1f);
                }
                __instance.carryWeight = LethalCP_VediciPlugin.currentWeight;
            }
        }
    }
}