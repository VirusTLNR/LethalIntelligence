using HarmonyLib;
using UnityEngine;

namespace LethalIntelligence.Patches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class MaskedPlayerEnemyPatch
    {
        public static MaskedAIRevamp vd;

        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        private static void Awake_Prefix(EnemyAI __instance)
        {
            if (Plugin.enableMaskedFeatures)
            {
                vd = ((Component)__instance).gameObject.AddComponent<MaskedAIRevamp>();
            }
            else if ((Object)(object)((Component)((Component)__instance).transform.GetChild(3).GetChild(0)).GetComponent<Animator>().runtimeAnimatorController != (Object)(object)Plugin.MapDotRework)
            {
                ((Component)((Component)__instance).transform.GetChild(3).GetChild(0)).GetComponent<Animator>().runtimeAnimatorController = Plugin.MapDotRework;
            }
        }

        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPrefix]
        private static bool OnCollideWithPlayer_Prefix()
        {
            return false;
        }

        [HarmonyPatch("LookAtPosition")]
        [HarmonyPrefix]
        private static bool LookAtPosition_Prefix()
        {
            return false;
        }

        [HarmonyPatch("LookAtPlayerServerRpc")]
        [HarmonyPrefix]
        private static bool LookAtPlayerServerRpc_Prefix()
        {
            return false;
        }

        //killing off zeekers code for masked teleporting at the main entrance... its causing a bug
        [HarmonyPatch("TeleportMaskedEnemyAndSync")]
        [HarmonyPrefix]
        private static bool TeleportMaskedEnemyAndSync_Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        [HarmonyPatch("SetMovingTowardsTargetPlayer")]
        [HarmonyPrefix]
        private static bool SetMovingTowardsTargetPlayer(EnemyAI __instance)
        {
            if(__instance.GetScriptClassName() == "MaskedPlayerEnemy")
            {
                Plugin.mls.LogError("THIS IS A MASKED");
                return false;
            }
            else
            {
                Plugin.mls.LogError("THIS IS NOT A MASKED");
                return true;
            }
        }
    }
}
