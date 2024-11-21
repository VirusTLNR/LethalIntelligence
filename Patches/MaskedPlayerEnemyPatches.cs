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
                if (Plugin.imperiumFound)
                {
                    ImperiumPatches.maskedVisualization();
                }
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

        //stop masked looking at weird places randomly.. also stops them running...
        [HarmonyPatch("LookAndRunRandomly")]
        [HarmonyPrefix]
        //almost complete replication of MaskedPlayerEnemy.LookAndRunRandomly, but disabling the random look.
        private static bool LookAndRunRandomly_Prefix(MaskedPlayerEnemy __instance, bool onlySetRunning = false)
        {
            __instance.randomLookTimer -= __instance.AIIntervalTime;
            if (!__instance.runningRandomly && !__instance.running)
            {
                __instance.staminaTimer = Mathf.Min(6f, __instance.staminaTimer + __instance.AIIntervalTime);
            }
            else
            {
                __instance.staminaTimer = Mathf.Max(0f, __instance.staminaTimer - __instance.AIIntervalTime);
            }
            if (__instance.randomLookTimer <= 0f)
            {
                __instance.randomLookTimer = Random.Range(0.7f, 5f);
                if (!__instance.runningRandomly)
                {
                    int num;
                    if (__instance.isOutside)
                    {
                        num = 35;
                    }
                    else
                    {
                        num = 20;
                    }
                    if (onlySetRunning)
                    {
                        num /= 3;
                    }
                    if (__instance.staminaTimer >= 5f && Random.Range(0, 100) < num)
                    {
                        __instance.running = true;
                        __instance.runningRandomly = true;
                        __instance.creatureAnimator.SetBool("Running", true);
                        __instance.SetRunningServerRpc(true);
                        return false;
                    }
                    if (onlySetRunning)
                    {
                        return false;
                    }
                    //stopping random direction looking.
                    /*Vector3 onUnitSphere = Random.onUnitSphere;
                    float num2 = 0f;
                    if (Physics.Raycast(__instance.eye.position, onUnitSphere, 5f, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers))
                    {
                        num2 = RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(__instance.eye.position, 12f, 5);
                    }
                    onUnitSphere.y = num2;
                    __instance.LookAtDirectionServerRpc(onUnitSphere, Random.Range(0.25f, 2f), Random.Range(-60f, 60f));*/
                    return false;
                }
                else
                {
                    int num3;
                    if (__instance.isOutside)
                    {
                        num3 = 80;
                    }
                    else
                    {
                        num3 = 30;
                    }
                    if (onlySetRunning)
                    {
                        num3 /= 5;
                    }
                    if (Random.Range(0, 100) > num3 || __instance.staminaTimer <= 0f)
                    {
                        __instance.running = false;
                        __instance.runningRandomly = false;
                        __instance.staminaTimer = -6f;
                        __instance.creatureAnimator.SetBool("Running", false);
                        __instance.SetRunningServerRpc(false);
                    }
                }
            }
            return false;
        }
    }
}
