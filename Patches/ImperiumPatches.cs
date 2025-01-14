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
                //.RegisterInsight("Running?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isRunning.Value}")
                //.RegisterInsight("Crouching?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isCrouched.Value}")
                //.RegisterInsight("Dancing?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isDancing.Value}")
                //.RegisterInsight("Jumping?", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().isJumped.Value}")
                //.RegisterInsight("ItemHeld", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().heldGrabbable}")
                /*.RegisterInsight("TPos", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().transform.rotation.ToString().Replace(" ", "")}")
                .RegisterInsight("TRot", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().transform.position.ToString().Replace(" ", "")}")*/
                .RegisterInsight("Pos", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedPosition.Value.ToString().Replace(" ", "")}")
                /*.RegisterInsight("RotX", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedRotation.Value.x.ToString("f5").Replace(" ", "")}")
                .RegisterInsight("RotY", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedRotation.Value.y.ToString("f5").Replace(" ", "")}")
                .RegisterInsight("RotZ", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedRotation.Value.z.ToString("f5").Replace(" ", "")}")
                .RegisterInsight("RotW", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().maskedRotation.Value.w.ToString("f5").Replace(" ", "")}")*/
                //.RegisterInsight("CurrentAreaMask", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().agent.areaMask}")
                .RegisterInsight("Dest", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().agent.pathEndPosition.ToString().Replace(" ", "")}")
                .RegisterInsight("Distance", entity => $"{entity.gameObject.GetComponent<MaskedAIRevamp>().currentDestinationDistance.Value.ToString().Replace("F", "")}")
                .RegisterInsight("HeadTilt", entity => $"{entity.headTiltTarget.eulerAngles.ToString()}")
                .RegisterInsight("VLookAngle", entity => $"{entity.verticalLookAngle.ToString()}");
                //.RegisterInsight("D->", entity => $"{MaskedPlayerEnemyPatch.vd.agent.destination.ToString()}");
                //.RegisterInsight("Goal", entity => $"{MaskedPlayerEnemyPatch.vd.maskedGoal}");
                ;
            }
            catch (NullReferenceException nre)
            {
                //masked was deleted/despawned.
            }
        }
    }
}
