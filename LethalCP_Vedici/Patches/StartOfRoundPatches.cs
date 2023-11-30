using HarmonyLib;
using System.Runtime.CompilerServices;

namespace LethalCP_Vedici.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatches
    {
        /// <summary>
        /// Display text box on the start of the game
        /// </summary>
        [HarmonyPatch(nameof(StartOfRound.openingDoorsSequence))]
        [HarmonyPostfix]
        static void customOpeningScreen()
        {
            if (LethalCP_VediciPlugin.isHost)
            {
                string noticeTitle = "Modded Game";
                string noticeBody = "Made with <3 by Stoichev";
                HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);
            }
        }
    }
}