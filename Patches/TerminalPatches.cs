using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalIntelligence.Patches
{
    internal class TerminalPatches
    {
        //taken from AutomaticSignals v1.3.0 by TestAccount666 (no edits made)
        public static class Transmitter
        {
            public static bool IsSignalTranslatorUnlocked()
            {
                return (from unlockableItem in StartOfRound.Instance.unlockablesList.unlockables
                        where !unlockableItem.alreadyUnlocked
                        where unlockableItem.hasBeenUnlockedByPlayer
                        select unlockableItem).Any((UnlockableItem unlockableItem) => unlockableItem.unlockableName.ToLower().Contains("translator"));
            }

            public static void SendMessage(string message)
            {
                HUDManager instance = HUDManager.Instance;
                SignalTranslator val = Object.FindObjectOfType<SignalTranslator>();
                val.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
                if (val.signalTranslatorCoroutine != null)
                {
                    ((MonoBehaviour)instance).StopCoroutine(val.signalTranslatorCoroutine);
                }
                message = message.Substring(0, Mathf.Min(message.Length, 10));
                Coroutine signalTranslatorCoroutine = ((MonoBehaviour)instance).StartCoroutine(instance.DisplaySignalTranslatorMessage(message, val.timesSendingMessage = Math.Max(val.timesSendingMessage + 1, 1), val));
                val.signalTranslatorCoroutine = signalTranslatorCoroutine;
            }
        }
    }
}
