using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LethalIntelligence.Patches
{
    internal class TerminalPatches
    {
        //all code in TerminalPatches.Transmitter is taken from AutomaticSignals v1.3.0 by TestAccount666 (no edits made, full credits to TestAccount666)
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

        //all code in TerminalPatches.Teleporter is taken from AutomaticSignals v1.3.0 by TestAccount666 (minor edits made for the purpose of this mod, full credits to TestAccount666)
        public static class Teleporter
        {
            private static IEnumerator BeamUpPlayer(PlayerControllerB? playerToTeleport, ShipTeleporter? teleporter)
            {
                if (teleporter == null)
                {
                    yield break;
                }
                teleporter.shipTeleporterAudio.PlayOneShot(teleporter.teleporterSpinSFX);
                if (playerToTeleport == null || playerToTeleport.deadBody != null)
                {
                    yield break;
                }
                teleporter.SetPlayerTeleporterId(playerToTeleport, 1);
                playerToTeleport.beamUpParticle.Play();
                playerToTeleport.movementAudio.PlayOneShot(teleporter.beamUpPlayerBodySFX);
                yield return (object)new WaitForSeconds(3f);
                if (playerToTeleport.deadBody == null)
                {
                    playerToTeleport.DropAllHeldItems(true, false);
                    AudioReverbPresets audioReverbPresets = Object.FindObjectOfType<AudioReverbPresets>();
                    if (audioReverbPresets != null)
                    {
                        audioReverbPresets.audioPresets[3].ChangeAudioReverbForPlayer(playerToTeleport);
                    }
                    playerToTeleport.isInElevator = true;
                    playerToTeleport.isInHangarShipRoom = true;
                    playerToTeleport.isInsideFactory = false;
                    playerToTeleport.averageVelocity = 0f;
                    playerToTeleport.velocityLastFrame = Vector3.zero;
                    playerToTeleport.TeleportPlayer(teleporter.teleporterPosition.position, true, 160f, false, true);
                    teleporter.SetPlayerTeleporterId(playerToTeleport, -1);
                    teleporter.shipTeleporterAudio.PlayOneShot(teleporter.teleporterBeamUpSFX);
                    if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
                    {
                        HUDManager.Instance.ShakeCamera((ScreenShakeType)1);
                    }
                }
            }
        }
    }
}
