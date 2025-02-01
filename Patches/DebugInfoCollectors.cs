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
    }
}
