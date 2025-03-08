using System;
using System.Collections.Generic;
using System.Text;

namespace LethalIntelligence.Patches
{
    public class DebugInfoCollectors
    {
        //just some methods for collecting information on the fly.. not to be used in live code.
        public static void BuyableItemsList(Terminal terminal)
        {
            Plugin.mls.LogError("BuyableItemsList DebugInfo");
            foreach(var item in terminal.buyableItemsList)
            {
                Plugin.mls.LogWarning(item.itemName);
            }
        }

        public static void logAllEntranceDetails(string pointInCode = "unknown")
        {
            Plugin.mls.LogError("logAllEntranceDetails Start @ " + pointInCode);
            EntranceTeleport[] entrancesTeleportArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            for (int i = 0; i < entrancesTeleportArray.Length; i++)
            {
                EntranceTeleport ent = entrancesTeleportArray[i];
                Plugin.mls.LogWarning("==============================" +
                    "\nEntrance # " + i +
                    //"\nEntName -> " + ent.name +
                    "\nEntID -> " + ent.entranceId +
                    "\nEntIsEntranceToBuilding (is outside?) -> " + ent.isEntranceToBuilding +
                    //"\nEntPosition -> " + ent.transform.position +
                    "\nEntEntrancePoint -> " + ent.entrancePoint.transform.position +
                    "\nEntExitPoint -> " + (ent.exitPoint == null ? "null" : ent.exitPoint.transform.position)
                    );
            }
            Plugin.mls.LogError("logAllEntranceDetails End @ " + pointInCode);
        }
    }
}
