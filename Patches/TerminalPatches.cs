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
        //checking for signal translator upgrade mod, not currently required as ive put in a fix that doesnt require checking for this
        //public static bool signalTranslatorUpgradeEnabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Fredolx.SignalTranslatorUpgrade"); } }

        //all code in TerminalPatches.Transmitter is taken from AutomaticSignals v1.3.0 by TestAccount666 (some edits made, otherwise full credits to TestAccount666)
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
                if (instance == null)
                {
                    Plugin.mls.LogFatal("(Transmitter.SendMessage) - HudManager is null");
                    return;
                }
                SignalTranslator val = Object.FindObjectOfType<SignalTranslator>();
                if (val == null)
                {
                    Plugin.mls.LogFatal("(Transmitter.SendMessage) - SignalTranslator is null");
                    return;
                }
                val.timeLastUsingSignalTranslator = Time.realtimeSinceStartup;
                if (val.signalTranslatorCoroutine != null)
                {
                    ((MonoBehaviour)instance).StopCoroutine(val.signalTranslatorCoroutine);
                }
                message = message.Substring(0, Mathf.Min(message.Length, 10));
                Coroutine signalTranslatorCoroutine = ((MonoBehaviour)instance).StartCoroutine(DisplaySignalTranslatorMessage(message, val.timesSendingMessage = Math.Max(val.timesSendingMessage + 1, 1), val));
                val.signalTranslatorCoroutine = signalTranslatorCoroutine;
            }

            //creating my own copy of DisplaySignalTranslatorMessage to avoid conflict with SignalTranslatorUpgrade
            private static IEnumerator DisplaySignalTranslatorMessage(string signalMessage, int seed, SignalTranslator signalTranslator)
            {
                HUDManager instance = HUDManager.Instance;
                if (instance == null)
                {
                    Plugin.mls.LogFatal("(Transmitter.DisplaySignalTranslatorMessage) - HudManager is null");
                    yield return new WaitForSeconds(0.5f);
                }
                System.Random signalMessageRandom = new System.Random(seed + StartOfRound.Instance.randomMapSeed);
                instance.signalTranslatorAnimator.SetBool("transmitting", true);
                signalTranslator.localAudio.Play();
                instance.UIAudio.PlayOneShot(signalTranslator.startTransmissionSFX, 1f);
                instance.signalTranslatorText.text = "";
                yield return new WaitForSeconds(1.21f);
                int i = 0;
                while (i < signalMessage.Length && !(signalTranslator == null) && signalTranslator.gameObject.activeSelf)
                {
                    instance.UIAudio.PlayOneShot(signalTranslator.typeTextClips[UnityEngine.Random.Range(0, signalTranslator.typeTextClips.Length)]);
                    instance.signalTranslatorText.text = instance.signalTranslatorText.text + signalMessage[i].ToString();
                    float num = Mathf.Min((float)signalMessageRandom.Next(-1, 4) * 0.5f, 0f);
                    yield return new WaitForSeconds(0.7f + num);
                    int num2 = i;
                    i = num2 + 1;
                }
                if (signalTranslator != null)
                {
                    instance.UIAudio.PlayOneShot(signalTranslator.finishTypingSFX);
                    signalTranslator.localAudio.Stop();
                }
                yield return new WaitForSeconds(0.5f);
                instance.signalTranslatorAnimator.SetBool("transmitting", false);
                yield break;
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
