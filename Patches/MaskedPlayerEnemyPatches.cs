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
            if (Plugin.enableMaskedFeatures && !badMaskedPlayerEnemy(__instance))
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

        public static bool badMaskedPlayerEnemy(EnemyAI __instance) //for auto disabling the MaskedAIRevamp component if certain circumstances are detected.
        {
            bool isBadMPE = false;
            switch(__instance.name)
            {
                case "MaskedPlayerEnemy":
                case "MaskedPlayerEnemy(Clone)":
                    isBadMPE =  false;
                    break;
                case "GhostPlayer": //Ooblterra's Ghost Player are often not on the navmesh.
                case "GhostPlayer(Clone)":
                    Plugin.mls.LogError("Ooblterra's GhostPlayer found, disabling MaskedAIRevamp component on " + __instance.name + "(" + __instance.GetInstanceID() + ")" + ". If you have issues with " + __instance.name + " (not acting as they were intended.) then report this error, otherwise ignore this error.");
                    isBadMPE = true;
                    break;
                default:
                    Plugin.mls.LogError("Unknown Potentially Unusable MaskedPlayerEnemy found with name: " + __instance.name + ". If you have issues with masked, report this error, otherwise ignore this error.");
                    isBadMPE = false;
                    break;
            }
            return isBadMPE;
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

        [HarmonyPatch("TeleportMaskedEnemy")]
        [HarmonyPrefix]
        private static bool TeleportMaskedEnemy_Prefix(MaskedPlayerEnemy __instance, Vector3 pos, bool setOutside)
        {
            __instance.timeAtLastUsingEntrance = Time.realtimeSinceStartup;
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos, default(UnityEngine.AI.NavMeshHit), 5f, -1);
            if (RoundManager.Instance.IsHost)
            {
                __instance.agent.enabled = false;
                __instance.agent.transform.position = navMeshPosition;
                ((Component)__instance).transform.position = navMeshPosition;
                __instance.agent.enabled = true;
            }
            else
            {
                ((Component)__instance).transform.position = navMeshPosition;
            }
            __instance.serverPosition = navMeshPosition;
            __instance.SetEnemyOutside(setOutside);
            EntranceTeleport entranceTeleport = findEntranceScript(setOutside, null, pos);
            if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
            {
                //both sides of the entrance should be heard.
                if(!entranceTeleport.FindExitPoint())
                {
                    return false;
                }
                entranceTeleport.PlayAudioAtTeleportPositions();
                WalkieTalkie.TransmitOneShotAudio(entranceTeleport.entrancePointAudio, entranceTeleport.doorAudios[0], 1f);
            }
            return false;
        }

        private static EntranceTeleport findEntranceScript(bool setOutside, int? id = null, Vector3? pos = null)
        {
            EntranceTeleport[] array = Object.FindObjectsOfType<EntranceTeleport>(false);
            for (int i = 0; i < array.Length; i++)
            {
                if (pos != null)
                {
                    //Plugin.mls.LogError("pos is NOT null!");
                    if (array[i].entrancePoint.position == pos && setOutside == array[i].isEntranceToBuilding)
                    {
                        //Plugin.mls.LogError("entrance found by Pos!= " + array[i].entranceId + "|outside?=" + array[i].isEntranceToBuilding + "|EntP=" + array[i].entrancePoint.position + "|ExitP=" + (array[i].exitPoint.position==null?"null": array[i].exitPoint.position));
                        return array[i];
                    }
                }
                if(id != null)
                {
                    //Plugin.mls.LogError("ID is NOT null!");
                    if (array[i].entranceId == id && setOutside == array[i].isEntranceToBuilding)
                    {
                        //Plugin.mls.LogError("entrance found by ID!= " + array[i].entranceId + "|outside?=" + array[i].isEntranceToBuilding + "|EntP=" + array[i].entrancePoint.position + "|ExitP=" + (array[i].exitPoint.position == null ? "null" : array[i].exitPoint.position));
                        return array[i];
                    }
                }
            }
            if (array.Length == 0)
            {
                Plugin.mls.LogError("No Entrances Exist, returning Null");
                return null;
            }
            Plugin.mls.LogError("Entrance at " + pos + " could not be found. Returning first entrance teleport script found.");
            return array[0];
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

        //trying to stop players looking up when they see a player
        /*[HarmonyPatch("LookAtPlayerClientRpc")]
        [HarmonyPostfix]
        private static void LookAtPlayerClientRpc_Postfix(MaskedPlayerEnemy __instance, int playerId)
        {
            Transform correctHeightPlayerTransform = StartOfRound.Instance.allPlayerScripts[playerId].transform;
            __instance.stareAtTransform = correctHeightPlayerTransform;
        }

        [HarmonyPatch("DetectNoise")]
        [HarmonyPrefix]
        private static bool DetectNoise_Prefix()
        {
            return false;
        }*/
    }
}