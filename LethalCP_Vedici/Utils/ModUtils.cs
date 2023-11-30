using GameNetcodeStuff;
using UnityEngine;

namespace LethalCP_Vedici.Utils
{
    /// <summary>
    /// Static utilities class for common functions and properties to be used within your mod code
    /// </summary>
    internal static class ModUtils
    {
        /// <summary>
        /// Method to find grabable items outside ship
        /// </summary>
        /// <param name="totalItems"></param>
        /// <param name="totalValue"></param>
        public static void findItemsOutsideShip(out int totalItems, out int totalValue)
        {
            System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 91);
            totalValue = 0;
            totalItems = 0;
            int x = 0;
            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].itemProperties.isScrap && !array[i].isInShipRoom && !array[i].isInElevator)
                {
                    x += array[i].itemProperties.maxValue - array[i].itemProperties.minValue;
                    totalValue += Mathf.Clamp(random.Next(array[i].itemProperties.minValue, array[i].itemProperties.maxValue), array[i].scrapValue - 6 * i, array[i].scrapValue + 9 * i);
                    totalItems++;
                }
            }
        }

        /// <summary>
        /// Method to find player status
        /// </summary>
        /// <param name="totalPlayerAlive"></param>
        /// <param name="totalPlayerDead"></param>
        public static void findTeamStatus(out int totalPlayerAlive, out int totalPlayerDead)
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
    }
}
