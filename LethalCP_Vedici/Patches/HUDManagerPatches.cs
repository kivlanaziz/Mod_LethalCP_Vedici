using HarmonyLib;
using System.Linq;
using System.Runtime.CompilerServices;
using LethalCP_Vedici.Utils;

namespace LethalCP_Vedici.Patches
{
    // TODO Review this file and update to your own requirements, or remove it altogether if not required

    /// <summary>
    /// Sample Harmony Patch class. Suggestion is to use one file per patched class
    /// though you can include multiple patch classes in one file.
    /// Below is included as an example, and should be replaced by classes and methods
    /// for your mod.
    /// </summary>
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatches
    {
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

                if (!LethalCP_VediciPlugin.isHost)
                {
                    noticeTitle = "Command";
                    noticeBody = "Unable to send command since you are not host.";
                    HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);
                    if (LethalCP_VediciPlugin.HideCommandMessages.Value)
                    {
                        __instance.chatTextField.text = "";
                    }
                    return;
                }
                if (text.ToLower().Contains("night") || text.ToLower().Contains("vision"))
                {
                    if (LethalCP_VediciPlugin.toggleNightVision())
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
                    ModUtils.findItemsOutsideShip(out totalItems, out totalValue);
                    noticeTitle = "Scan Result";
                    noticeBody = $"There are {totalItems} objects outside the ship, totalling at an approximate value of ${totalValue}.";
                }
                if (text.ToLower().Contains("player"))
                {
                    int totalPlayerAlive = 0, totalPlayerDead = 0;
                    ModUtils.findTeamStatus(out totalPlayerAlive, out totalPlayerDead);
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
                if (LethalCP_VediciPlugin.HideCommandMessages.Value)
                {
                    __instance.chatTextField.text = "";
                }
                return;
            }
        }
    }
}