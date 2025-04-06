using System;
using System.Collections.Generic;
using System.Text;
using Imperium.API;
using UnityEngine.AI;

namespace LethalIntelligence.Patches
{
    internal class ImperiumPatches
    {
        public static void maskedVisualization()
        {
            try
            {
                Visualization.InsightsFor<MaskedPlayerEnemy>()
                // Registers a new insight generator
                .RegisterInsight("Personality", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedPersonality}")
                .RegisterInsight("Focus", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedFocus}")
                .RegisterInsight("Activity", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedActivity}")
                .RegisterInsight("PlayerInterest", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().interestInPlayers.Value}")
                //.RegisterInsight("Running?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isRunning.Value}")
                //.RegisterInsight("Crouching?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isCrouched.Value}")
                //.RegisterInsight("Dancing?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isDancing.Value}")
                //.RegisterInsight("Jumping?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isJumped.Value}")
                //.RegisterInsight("ItemHeld", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().heldGrabbable}")
                .RegisterInsight("Pos", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().transform.position.ToString().Replace(" ", "")}")
                .RegisterInsight("CurrentAreaMask", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().agent.areaMask}")
                .RegisterInsight("Dest", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().agent.pathEndPosition.ToString().Replace(" ", "")}");
                //.RegisterInsight("D->", entity => $"{MaskedPlayerEnemyPatch.vd.agent.destination.ToString()}");
                //.RegisterInsight("Goal", entity => $"{MaskedPlayerEnemyPatch.vd.maskedGoal}");
            }
            catch (NullReferenceException nre)
            {
                //masked was deleted/despawned.
            }
        }
    }
}
