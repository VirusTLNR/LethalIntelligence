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
            Visualization.InsightFor<MaskedPlayerEnemy>()
            // Registers a new insight generator
            .RegisterInsight("Personality", entity => $"{p}")
            .RegisterInsight("Focus", entity => $"{f}")
            .RegisterInsight("Activity", entity => $"{a}");
        }
    }
}
