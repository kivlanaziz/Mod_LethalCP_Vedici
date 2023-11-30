using HarmonyLib;
using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

namespace LethalCP_Vedici.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(TimeOfDay))]
    internal class TimeOfDayPatches
    {
        /// <summary>
        /// Method to update time scale using custom modifier
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(TimeOfDay.MoveGlobalTime))]
        [HarmonyPostfix]
        static void customizableTimeScale(TimeOfDay __instance)
        {
            if (LethalCP_VediciPlugin.isHost)
            {
                if (LethalCP_VediciPlugin.CustomTimeScale.Value)
                {
                    __instance.globalTimeSpeedMultiplier = LethalCP_VediciPlugin.MaximumTimeScale.Value;

                    if (LethalCP_VediciPlugin.UseRandomTimeScale.Value)
                    {
                        __instance.globalTimeSpeedMultiplier = Random.Range(LethalCP_VediciPlugin.MinimumTimeScale.Value, LethalCP_VediciPlugin.MaximumTimeScale.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Method to patch Global Time for infinite deadline
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(TimeOfDay.MoveGlobalTime))]
        [HarmonyPrefix]
        static void InfiniteDeadline(ref float ___timeUntilDeadline)
        {
            if (!LethalCP_VediciPlugin.isHost) { return; }
            if (LethalCP_VediciPlugin.EnableCustomDeadline.Value)
            {
                ___timeUntilDeadline = 999;
            }

        }
    }
}