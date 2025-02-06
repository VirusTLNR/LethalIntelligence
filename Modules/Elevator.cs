using LethalIntelligence.Patches;
using UnityEngine;

namespace LethalIntelligence.Modules
{
    internal static class Elevator
    {
        internal static bool HandleElevators(MaskedPlayerEnemy maskedEnemy, MaskedAIRevamp mair, string currentMoon, string currentInterior)
        {
            if (ElevatorFirst(maskedEnemy, mair, currentMoon, currentInterior))
            {
                mair.isRunning.Value = false;
                if (currentInterior == "Level3Flow")
                {
                    bool hasElevator = true;
                    if (maskedEnemy.elevatorScript == null)
                    {
                        maskedEnemy.elevatorScript = Object.FindObjectOfType<MineshaftElevatorController>();
                        if (maskedEnemy.elevatorScript == null)
                        {
                            hasElevator = false;
                        }
                    }
                    if (hasElevator)
                    {
                        if (maskedEnemy.isInElevatorStartRoom)
                        {
                            if (Vector3.Distance(maskedEnemy.transform.position, maskedEnemy.elevatorScript.elevatorBottomPoint.position) < 10f)
                            {
                                maskedEnemy.isInElevatorStartRoom = false;
                            }
                        }
                        else if (Vector3.Distance(maskedEnemy.transform.position, maskedEnemy.elevatorScript.elevatorTopPoint.position) < 10f)
                        {
                            maskedEnemy.isInElevatorStartRoom = true;
                        }
                    }
                    //maskedEnemy.UseElevator(!maskedEnemy.isInElevatorStartRoom);
                    return UseElevator(maskedEnemy, mair, currentMoon, currentInterior);
                }
            }
            return true;
        }

        private static bool ElevatorFirst(MaskedPlayerEnemy maskedEnemy, MaskedAIRevamp mair, string currentMoon, string currentInterior)
        {
            if (mair.maskedFocus != MaskedAIRevamp.Focus.Player)
            {
                if (currentInterior == "Level3Flow")
                {
                    if (!maskedEnemy.isOutside && !maskedEnemy.isInElevatorStartRoom && mair.maskedActivity == MaskedAIRevamp.Activity.MainEntrance && mair.maskedFocus != MaskedAIRevamp.Focus.Player)
                    {
                        return true; //mineshaft + inside + not in elevator start room + heading to main entrance and not focusing on players
                    }
                    if (!maskedEnemy.isOutside && maskedEnemy.isInElevatorStartRoom && mair.maskedActivity != MaskedAIRevamp.Activity.MainEntrance && mair.maskedFocus != MaskedAIRevamp.Focus.Player)
                    {
                        return true; //mineshaft + inside + not in elevator start room + heading away from main entrance and not focusing on players
                    }
                }
                if (currentMoon == "SynthesisLevel")
                {
                    //masked phase through the elevator and use the ladder below so no scripting required
                }
                if (currentInterior == "OfficeDungeonFlow")
                {
                    //no elevator script so masked cant use the elevator
                }
            }
            return false;
        }

        //buttery's implementation of UseElevator and CanPathToPoint with some tweaks to fit my code
        private static bool CanPathToPoint(MaskedPlayerEnemy maskedAI, Vector3 pos)
        {
            maskedAI.pathDistance = 0f; // for consistency with vanilla

            if (maskedAI.agent.isOnNavMesh && !maskedAI.agent.CalculatePath(pos, maskedAI.path1))
                return false;

            if (maskedAI.path1 == null || maskedAI.path1.corners.Length == 0)
                return false;

            if (Vector3.Distance(maskedAI.path1.corners[^1], RoundManager.Instance.GetNavMeshPosition(pos, RoundManager.Instance.navHit, 2.7f, maskedAI.agent.areaMask)) > 1.5f)
                return false;

            return true;
        }

        static bool elevatorHasRider = false;

        // return true if elevator is no longer needed
        // return false (delaying pathfinding) if need to take elevator still
        private static bool UseElevator(MaskedPlayerEnemy maskedAI, MaskedAIRevamp mair, string currentMoon, string currentInterior)
        {
            if (maskedAI.elevatorScript == null)
            {
                return true;
            }
            Vector3 moveInsidePointFurtherBack = -maskedAI.elevatorScript.elevatorInsidePoint.forward * 0.25f;
            Vector3 elevatorCustomInsidePoint = maskedAI.elevatorScript.elevatorInsidePoint.position + moveInsidePointFurtherBack;

            bool ridingElevator = Vector3.Distance(maskedAI.transform.position, elevatorCustomInsidePoint) < 1f;

            // if we're already in the elevator, wait until it is done moving
            if (ridingElevator && (!maskedAI.elevatorScript.elevatorFinishedMoving || !maskedAI.elevatorScript.elevatorDoorOpen))
            {
                //maskedAI.SetDestinationToPosition(maskedAI.elevatorScript.elevatorInsidePoint.position);
                return false;
            }

            //maskedAI.isInElevatorStartRoom = startRoomBounds.Contains(maskedAI.transform.position);
            Vector3 elevatorOutsidePoint = maskedAI.isInElevatorStartRoom ? maskedAI.elevatorScript.elevatorTopPoint.position : maskedAI.elevatorScript.elevatorBottomPoint.position;

            // if trying to go outside, we need to be in the start room
            bool needToUseElevator = ElevatorFirst(maskedAI, mair, currentMoon, currentInterior);
            // otherwise, check if we can target someone where we are
            /*if (!needToUseElevator)
            {
                bool closestPlayerAtTop = startRoomBounds.Contains(closestInsidePlayer.transform.position);
                if (maskedAI.isInElevatorStartRoom != closestPlayerAtTop)
                    needToUseElevator = true;
            }*/

            // don't need to use elevator, and we're not on the elevator, so all is good
            if (!needToUseElevator && !ridingElevator)
                return true;

            // are we already in the elevator?
            if (ridingElevator)
            {
                elevatorHasRider = true;
                // elevator is in the wrong position, need to press the button
                if (needToUseElevator && maskedAI.elevatorScript.elevatorMovingDown != maskedAI.isInElevatorStartRoom)
                {
                    maskedAI.elevatorScript.PressElevatorButtonOnServer(true);
                    return false;
                }

                // otherwise, try to get off the elevator
                if (!needToUseElevator && maskedAI.elevatorScript.elevatorFinishedMoving && maskedAI.elevatorScript.elevatorDoorOpen)
                {
                    if (CanPathToPoint(maskedAI, elevatorOutsidePoint))
                    {
                        maskedAI.SetDestinationToPosition(elevatorOutsidePoint);
                        new WaitForSeconds(2f);
                        elevatorHasRider = false;
                    }
                    return false;
                }
            }

            if (needToUseElevator)
            {
                // if elevator is here, get on board
                if (maskedAI.elevatorScript.elevatorFinishedMoving && maskedAI.elevatorScript.elevatorDoorOpen && maskedAI.elevatorScript.elevatorMovingDown != maskedAI.isInElevatorStartRoom)
                {
                    if (CanPathToPoint(maskedAI, elevatorCustomInsidePoint))
                        maskedAI.SetDestinationToPosition(elevatorCustomInsidePoint);

                    return false;
                }

                // are we already standing at the elevator?
                if (Vector3.Distance(maskedAI.transform.position, elevatorOutsidePoint) < 1f && !elevatorHasRider)
                {
                    // need to call the elevator when we can
                    if (maskedAI.elevatorScript.elevatorMovingDown == maskedAI.isInElevatorStartRoom && !maskedAI.elevatorScript.elevatorCalled)
                        maskedAI.elevatorScript.CallElevator(!maskedAI.isInElevatorStartRoom);

                    return false;
                }

                // need to walk towards the elevator
                if (CanPathToPoint(maskedAI, elevatorOutsidePoint))
                    maskedAI.SetDestinationToPosition(elevatorOutsidePoint);

                return false;
            }

            Plugin.mls.LogWarning("Masked elevator behavior seems to be stuck in an impossible state");
            return false;
        }
    }
}
