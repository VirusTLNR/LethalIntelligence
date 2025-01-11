﻿using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace LethalIntelligence.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {

        /// <summary>
        /// should maybe change this to check each entrance vs all the others.. if the entrance has ONE+ complete path's then it is fine to use, check inside and outside at the same time, i guess.
        /// </summary>
        private static EntranceTeleport[] entrancesTeleportArray = null!;
        private static List<EntranceTeleport> eta = new List<EntranceTeleport>();
        private static List<string> badCombinations = new List<string>();
        private static List<string> goodCombinations = new List<string>();
        private static NavMeshPath path;
        private static int matchesChecked = 0, entrancesChecked = 0;
        public static List<int> invalidEntrances = new List<int>();
        private static Vector3 lastPos;

        public static LethalNetworkAPI.LNetworkVariable<List<int>> networkedInvalidEntrances = LethalNetworkAPI.LNetworkVariable<List<int>>.Connect("networkedInvalidEntrances" + StartOfRound.Instance.NetworkObjectId, null, LethalNetworkAPI.LNetworkVariableWritePerms.Server);

        private static bool isEntrancePathComplete(Vector3 s, int d)
        {
            path = new NavMeshPath();
            NavMeshHit hit1, hit2;
            NavMesh.SamplePosition(s, out hit1, 1.0f, -1);
            int attempt = 1;
            float dist = 1000f;
            Vector3 currPos = hit1.position;
            while (attempt >= 1 && attempt <= 20 && dist > 1.5f)
            {
                lastPos = currPos;
                NavMesh.SamplePosition(currPos, out hit1, 1.0f, -1);
                NavMesh.SamplePosition(entrancesTeleportArray[d].entrancePoint.position, out hit2, 1.0f, -1);
                NavMesh.CalculatePath(hit1.position, hit2.position, -1, path);
                dist = Vector3.Distance(currPos, hit2.position);
                Plugin.mls.LogDebug("entrance#=" + d + " ID=" + entrancesTeleportArray[d].entranceId + " | dist=" + dist.ToString() + " | corners=" + path.corners.Length.ToString() + " | attempt=" + attempt.ToString() + " | status=" + path.status.ToString());
                attempt++;
                if (path.corners.Length == 0)
                {
                    return false;
                }
                currPos = path.corners[path.corners.Length - 1];
                if (currPos == lastPos)
                {
                    attempt = -1;
                }
            };
            if (path.status == NavMeshPathStatus.PathComplete) return true;
            return false;
        }


        private static void checkPathtoRandomLocation(Vector3 et1, int et2)
        {
            string status;
            bool doorSuccess = isEntrancePathComplete(et1, et2);// = calcPath(et1,et2);
            if (doorSuccess)
            {
                goodCombinations.Add(entrancesTeleportArray[et2].isEntranceToBuilding + "|" + entrancesTeleportArray[et2].entranceId);
                //goodCombinations.Add(entrancesTeleportArray[et2].isEntranceToBuilding + "|" + fixETID(et2));
                status = "PathComplete";
            }
            else
            {
                badCombinations.Add(entrancesTeleportArray[et2].isEntranceToBuilding + "|" + entrancesTeleportArray[et2].entranceId);
                //badCombinations.Add(entrancesTeleportArray[et2].isEntranceToBuilding + "|" + fixETID(et2));
                status = "PathInvalid";
            }
            Plugin.mls.LogDebug(entrancesTeleportArray[et2].isEntranceToBuilding.ToString().Replace("True", "Outside").Replace("False", "Inside") + " RandomPosition(" + et1.ToString() + ") --> " + entrancesTeleportArray[et2].entranceId + " = " + status);
            //Plugin.mls.LogDebug(entrancesTeleportArray[et2].isEntranceToBuilding.ToString().Replace("True", "Outside").Replace("False", "Inside") + " RandomPosition(" + et1.ToString() + ") --> " + fixETID(et2) + " = " + status);
        }

        private static void analysePathingData()
        {
            int[] outsideCount = new int[(entrancesTeleportArray.Length / 2)];
            int[] insideCount = new int[(entrancesTeleportArray.Length / 2)];
            int numBadsIsInvalid = matchesChecked/entrancesChecked;

            foreach (string bc in badCombinations)
            {
                string[] parts = bc.Split(new char[] { '|' });
                if (parts[0] == "True")
                {
                    outsideCount[int.Parse(parts[1])] += 1;
                }
                else
                {
                    insideCount[int.Parse(parts[1])] += 1;
                }
            }
            int c = 0;
            foreach (int num in outsideCount)
            {
                Plugin.mls.LogDebug("Outside Entrance #" + c + " = " + num + "/" + numBadsIsInvalid);
                if (num >= numBadsIsInvalid)
                {
                    Plugin.mls.LogWarning("Entrance #" + c + " is invalid for AI routing due to pathing issues Outside - this EntranceTeleport will be ignored for this round.");
                    if (!invalidEntrances.Contains(c))
                    {
                        invalidEntrances.Add(c);
                    }
                }
                c++;
            }
            int d = 0;
            foreach (int num in insideCount)
            {
                Plugin.mls.LogDebug("Inside Entrance #" + d + " = " + num + "/" + numBadsIsInvalid);
                if (num >= numBadsIsInvalid)
                {
                    Plugin.mls.LogWarning("Entrance #" + d + " is invalid for AI routing due to pathing issues Inside - this EntranceTeleport will be ignored for this round.");
                    if (!invalidEntrances.Contains(d))
                    {
                        invalidEntrances.Add(d);
                    }
                }
                d++;
            }
            networkedInvalidEntrances.Value = invalidEntrances;
        }

        private static int fixETID(int arrayIndex)
        {
            int value = -1;
            //value = (int)System.Math.Floor((decimal)arrayIndex / 2); //not working as intended
            int halfArrayLength = entrancesTeleportArray.Length / 2;
            if (arrayIndex >= halfArrayLength)
            {
                value = arrayIndex - halfArrayLength;
            }
            else
            {
                value = arrayIndex;
            }
            return value;
        }

        //other objects
        /*private static void checkPathToObject(int et1, Vector3 et2, string obj)
        {
            string status;
            bool objectSuccess = isObjectPathComplete(et1, et2);
            if (objectSuccess)
            {
                goodCombinations.Add(entrancesTeleportArray[et1].isEntranceToBuilding + "|" + fixETID(et1) + "|" + obj);
                status = "PathComplete";
            }
            else
            {
                badCombinations.Add(entrancesTeleportArray[et1].isEntranceToBuilding + "|" + fixETID(et1) + "|" + obj);
                status = "PathInvalid";
            }
            Plugin.mls.LogError(entrancesTeleportArray[et1].isEntranceToBuilding.ToString() + fixETID(et1) + "-->" + obj + " = " + status);
        }*/

        //private static bool isObjectPathComplete(int s, Vector3 d)
        //{
        //    path = new NavMeshPath();
        //    NavMeshHit hit1, hit2;
        //    NavMesh.SamplePosition(entrancesTeleportArray[s].entrancePoint.position, out hit1, 1.0f, -1);
        //    /*NavMesh.SamplePosition(entrancesTeleportArray[d].entrancePoint.position, out hit2, 1.0f, -1);
        //    NavMesh.CalculatePath(hit1.position, hit2.position, -1, path);*/
        //    int attempt = 1;
        //    float dist = 1000f;
        //    Vector3 currPos = hit1.position;
        //    while (attempt >= 1 && attempt <= 20 && dist > 1.5f)
        //    {
        //        lastPos = currPos;
        //        NavMesh.SamplePosition(currPos, out hit1, 1.0f, -1);
        //        //NavMesh.SamplePosition(d, out hit2, 1.0f, -1);
        //        NavMesh.CalculatePath(hit1.position, d, -1, path);
        //        dist = Vector3.Distance(currPos, d);
        //        Plugin.mls.LogWarning("ent= " + s + " | id=" + entrancesTeleportArray[s].entranceId + " | " + "dist=" + dist.ToString() + "| corners=" + path.corners.Length.ToString() + "| attempt=" + attempt.ToString() + "|status =" + path.status.ToString());
        //        attempt++;
        //        if (path.corners.Length == 0)
        //        {
        //            return false;
        //        }
        //        currPos = path.corners[path.corners.Length - 1];
        //        if (currPos == lastPos)
        //        {
        //            attempt = -1;
        //        }
        //    };
        //    if (path.status == NavMeshPathStatus.PathComplete) return true;
        //    return false;
        //}


        public static Vector3 RandomNavmeshLocation(Vector3 startingPoint, Vector3 spFront, float move, float radius)
        {
            float distance = 0f;
            while (distance < (radius * 0.75))
            {
                Vector3 randomDirection = Random.insideUnitSphere * radius;
                randomDirection += startingPoint;
                NavMeshHit hit;
                Vector3 finalPosition = Vector3.zero;
                if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
                {
                    finalPosition = hit.position;
                }
                distance = Vector3.Distance(finalPosition, startingPoint);
                if (distance > (radius * 0.70))
                {
                    return finalPosition;
                }
            }
            return startingPoint;
        }

        private static void earlyCallSetExitIDs()
        {
            float startTime = Time.timeSinceLevelLoad;
            while (RoundManager.FindMainEntrancePosition(false, false) == Vector3.zero && Time.timeSinceLevelLoad - startTime < 15f)
            {
                new WaitForSeconds(1f);
            }
            Vector3 mep = RoundManager.FindMainEntrancePosition(false, false);
            RoundManager.Instance.SetExitIDs(mep);
        }


        [HarmonyPostfix]
        [HarmonyPatch("SpawnSyncedProps")] //works but requires calling "SetExitIDs" early
        //[HarmonyPatch("SetExitIDs")] //works but comes after door locking (not good!)
        public static void PathingAccessibilityTestPostfix()
        {
            if (GameNetworkManager.Instance.isHostingGame)
            {
                if (RoundManager.Instance.currentLevel.name.ToString() == "CompanyBuildingLevel")
                {//company moon (no interior)
                    return; //prevent this code running on the company
                }
                Plugin.mls.LogInfo("Checking which Entrance Teleports are Valid...");
                earlyCallSetExitIDs();
                entrancesChecked = 0;
                matchesChecked = 0;
                badCombinations.Clear();
                goodCombinations.Clear();
                entrancesTeleportArray = Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
                for (int i = 0; i < entrancesTeleportArray.Length; i++)
                {
                    /*Plugin.mls.LogError("EntranceDetails for #" + i);
                    Plugin.mls.LogWarning("Prefab           =" + entrancesTeleportArray[i]);
                    Plugin.mls.LogWarning("ID               =" + entrancesTeleportArray[i].entranceId);
                    Plugin.mls.LogWarning("Outside?         =" + entrancesTeleportArray[i].isEntranceToBuilding);
                    Plugin.mls.LogWarning("EntrancePointPos =" + entrancesTeleportArray[i].entrancePoint.position);
                    if (entrancesTeleportArray[i].FindExitPoint())
                    {
                        Plugin.mls.LogWarning("ExitPointPos     =" + entrancesTeleportArray[i].exitPoint.position);
                    }
                    else
                    {
                        Plugin.mls.LogWarning("ExitPointPos     =" + "null");
                    }
                    Plugin.mls.LogWarning("TransformPos     =" + entrancesTeleportArray[i].transform.position);*/

                    if (entrancesTeleportArray[i] == null)
                    {
                        continue;
                    }
                    for (int t = 0; t < 3; t++)
                    {
                        Vector3 randomLocation;
                        if (entrancesTeleportArray[i].isEntranceToBuilding)
                        {
                            //use ship location
                            GameObject[] array = GameObject.FindGameObjectsWithTag("OutsideAINode");
                            randomLocation = array[Random.RandomRangeInt(0, array.Length)].transform.position;
                        }
                        else
                        {
                            //randomLocation = RoundManager.Instance.allEnemyVents[Random.RandomRangeInt(0, RoundManager.Instance.allEnemyVents.Length)].transform.position;
                            randomLocation = RandomNavmeshLocation(entrancesTeleportArray[i].entrancePoint.position, entrancesTeleportArray[i].entrancePoint.right, 15f, 5f);
                        }
                        Plugin.mls.LogDebug("New RandomLocation Set=" + randomLocation.ToString());
                        checkPathtoRandomLocation(randomLocation, i);
                        matchesChecked++;
                    }
                    entrancesChecked++;
                    //check versus other entrances
                    /*for (int j = 0; j < entrancesTeleportArray.Length; j++)
                    {
                        if (entrancesTeleportArray[j] == null)
                        {
                            continue;
                        }
                        if (entrancesTeleportArray[i].isEntranceToBuilding == entrancesTeleportArray[j].isEntranceToBuilding)
                        {
                            if (entrancesTeleportArray[i] == entrancesTeleportArray[j])
                            {
                                continue; //same entrance/exit, do not compare.
                            }
                            matchesChecked++;
                            checkPathBetweenEntrances(i, j);
                        }
                    }*/

                    /*NavMeshHit oohit;
                    BreakerBox breaker;
                    Terminal terminal;
                    LungProp apparatus;
                    Vector3 lockerPosition;

                    //check versus other destinations
                    //breaker-inside
                    breaker = GameNetworkManager.Instance.GetComponent<BreakerBox>();
                    if (NavMesh.SamplePosition(breaker.transform.position + -breaker.transform.up, out oohit, 10f, -1))
                    {
                        checkPathToObject(i, oohit.position, "breaker");
                    }

                    //apparatus-inside
                    apparatus = Object.FindObjectOfType<LungProp>();
                    if (NavMesh.SamplePosition(apparatus.transform.position + -apparatus.transform.forward, out oohit, 10f, -1))
                    {
                        checkPathToObject(i, oohit.position, "apparatus");
                    }

                    //ships locker-outside
                    lockerPosition = GameObject.Find("LockerAudio").transform.position;
                    checkPathToObject(i, oohit.position, "locker");

                    //ships terminal-outside
                    terminal = GameNetworkManager.Instance.GetComponent<Terminal>();

                    if (NavMesh.SamplePosition(new Vector3(terminal.transform.position.x, terminal.transform.position.y - 1.44f, terminal.transform.position.z) - (terminal.transform.right * 0.8f), out oohit, 10f, -1))
                    {
                        checkPathToObject(i, oohit.position, "terminal");
                    }*/

                }
                analysePathingData();
                Plugin.mls.LogInfo("Entrance Teleport Checks completed.");
            }
        }
    }
}
