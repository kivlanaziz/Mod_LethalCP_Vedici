using HarmonyLib;

namespace LethalCP_Vedici.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatches
    {
        /// <summary>
        /// Method to patch load new level function
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(RoundManager.LoadNewLevel))]
        [HarmonyPrefix]
        static void LoadNewLevelPrefixPatch(RoundManager __instance, SelectableLevel newLevel)
        {
            LethalCP_VediciPlugin.currentLevel = newLevel;
            if (LethalCP_VediciPlugin.UseCustomMapSettings.Value)
            {
                if (LethalCP_VediciPlugin.resetCustomMultiplier)
                {
                    LethalCP_VediciPlugin.Log.LogInfo("Resetting Custom Multiplier Value");
                    __instance.scrapAmountMultiplier /= LethalCP_VediciPlugin.CustomScrapAmountMultiplier.Value;
                    __instance.scrapValueMultiplier /= LethalCP_VediciPlugin.CustomScrapValueMultiplier.Value;
                    __instance.mapSizeMultiplier /= LethalCP_VediciPlugin.CustomMapSizeMultiplier.Value;
                }

                __instance.scrapAmountMultiplier *= LethalCP_VediciPlugin.CustomScrapAmountMultiplier.Value;
                __instance.scrapValueMultiplier *= LethalCP_VediciPlugin.CustomScrapValueMultiplier.Value;
                __instance.mapSizeMultiplier *= LethalCP_VediciPlugin.CustomMapSizeMultiplier.Value;
                LethalCP_VediciPlugin.resetCustomMultiplier = true;
            }
        }
    }
}