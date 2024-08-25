using System;
using System.Collections.Generic;
using System.Text;
using Imperium.API;
using UnityEngine.AI;

namespace LethalIntelligence.Patches
{
    internal class ImperiumPatches
    {
        public static void maskedVisualization(MaskedAIRevamp.Personality p,MaskedAIRevamp.Activity a,MaskedAIRevamp.Focus f)
        {
            try
            {
                Visualization.InsightsFor<MaskedPlayerEnemy>()
                // Registers a new insight generator
                .RegisterInsight("Personality", entity => $"{p}")
                .RegisterInsight("Focus", entity => $"{f}")
                .RegisterInsight("Activity", entity => $"{a}")
                .RegisterInsight("D->", entity => $"{MaskedPlayerEnemyPatch.vd.agent.destination.ToString()}");
                //.RegisterInsight("Goal", entity => $"{MaskedPlayerEnemyPatch.vd.maskedGoal}");
            }
            catch(NullReferenceException nre)
            {
                //masked was deleted/despawned.
            }
        }
    }
}
