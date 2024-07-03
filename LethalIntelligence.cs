using LobbyCompatibility.Attributes;
using LobbyCompatibility.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using LethalIntelligence.Patches;
using Microsoft.CodeAnalysis;
//using SkinwalkerMod;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using LobbyCompatibility.Features;

namespace LethalIntelligence
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        internal new static ManualLogSource mls { get; private set; } = null!;
        internal static Harmony? harmony { get; set; }

        //debug config so potentially spammy logs can be contained and prevented.
        public static bool DebugMode = false;
        public static string LastDebugModeMsg;

        //variables for mod
        //====================
        public static string logPluginName = "Lethal Intelligence";
        public static AssetBundle Bundle;

        //config settings
            //gen
        public static bool enableMaskedFeatures, enableSkinWalkers, enableWendigos;

            //masked personalities
        public static bool enableMaskedAggressive, enableMaskedStealthy, enableMaskedCunning, enableMaskedDeceiving, enableMaskedInsane;

            //masked config
        public static bool useTerminal, useTerminalCredit, maskedShipDeparture;

        public static GameObject MapDotPrefab;
        public static RuntimeAnimatorController MaskedAnimController;
        public static RuntimeAnimatorController MapDotRework;
        public static string PluginDirectory;
        public static bool skinWalkersIntegrated;
        public static bool wendigosIntegrated;
        public static bool moreEmotesIntegrated;

        public static bool debugModeSetting;
        public static int debugStatusDelay;
        //end of variables for mod

        //variables so all mobs know these..
        internal static bool isTerminalBeingUsed = false;
        internal static bool isBreakerBoxBeingUsed = false;
        internal static bool imperiumFound;

        private void Awake()
        {
            if ((Object)(object)Instance == (Object)null)
            {
                Instance = this;
            }
            mls = base.Logger;

            if (LobbyCompatibilityChecker.Enabled)
            {
                mls.LogInfo($"BMX.LobbyCompatibility has been found, Initiating Soft Dependency!");
                LobbyCompatibilityChecker.Init();
            }

            imperiumFound = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("giosuel.Imperium");

            PluginDirectory = ((BaseUnityPlugin)this).Info.Location;
            LoadAssets();
            mls.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");

            //loading config
            //general settings
            enableMaskedFeatures = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Masked AI Features", true, "Turn on masked AI features. If this feature is disabled, it will only change Masked's radar movement. If all masked Personalities are disabled, this will be disabled by default. *This option must be enabled to change Masked's AI.*").Value;
            enableSkinWalkers = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "SkinWalkers mod Compatibility", true, "Enables compatibility with the SkinWalkers mod. (Requires SkinWalkers mod installed, automatically disables on launch if not installed)").Value;
            enableWendigos = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Wendigos mod Compatibility", true, "Enables compatibility with the Wendigos_Voice_Cloning mod. (Requires Wendigos_Voice_Cloning mod installed, automatically disables on launch if not installed)").Value;

            //maskedPersonality
            enableMaskedAggressive = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedPersonalities", "MaskedAggressive", true, "Enables the 'Aggressive' personality for the Masked (at least 1 of these must be TRUE)").Value;
            enableMaskedStealthy = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedPersonalities", "MaskedStealthy", true, "Enables the 'Stealthy' personality for the Masked (at least 1 of these must be TRUE)").Value;
            enableMaskedCunning = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedPersonalities", "MaskedCunning", true, "Enables the Cunning personality for the Masked (at least 1 of these must be TRUE)").Value;
            enableMaskedDeceiving = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedPersonalities", "MaskedDeceiving", true, "Enables the 'Deceiving' personality for the Masked (at least 1 of these must be TRUE)").Value;
            enableMaskedInsane = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedPersonalities", "MaskedInsane", true, "Enables the 'Insane' personality for the Masked (at least 1 of these must be TRUE)").Value;

            if(!enableMaskedAggressive && !enableMaskedStealthy && !enableMaskedCunning && !enableMaskedDeceiving && !enableMaskedInsane)
            {
                enableMaskedFeatures = false;
                mls.LogWarning("Bad Config!, all Masked personalities are disabled, disabling entire MaskedAI Module.");
            }

            //masked settings
            useTerminal = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedAI", "Masked terminal access", true, "Allows Masked to use the terminal.").Value;
            useTerminalCredit = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedAI", "Masked uses credits", false, "(Not working rn) Allows Masked to use the terminal to spend credits.").Value;
            maskedShipDeparture = ((BaseUnityPlugin)this).Config.Bind<bool>("MaskedAI", "Masked pulls the brake lever", true, "Allows Masked to pull the brake lever.").Value;

            //debug settings
            debugModeSetting = ((BaseUnityPlugin)this).Config.Bind<bool>("DebugMode", "Debug Mode", false, "Enables more spammy logs for debugging, will be enabled automatically if imperium is installed. (all other DebugMode settings are ignored if Debug Mode is disabled)").Value;
            debugStatusDelay = ((BaseUnityPlugin)this).Config.Bind<int>("DebugMode", "Status Report Delay", 100, "How often should status reports (only updates when information changes) be logged (higher number = less spam but also less accurate as not all information is gathered").Value;
            if (imperiumFound || debugModeSetting)
            {
                if (imperiumFound)
                {
                    mls.LogWarning($"Imperium has been found, All Hail The Emperor!, Auto Initiating Debug Mode (More Logs!)");
                }
                else
                {
                    mls.LogWarning($"Debug Mode enabled in config (More Logs!)");
                }
                DebugMode = true;
            }
            Patch();
            /*Logger = base.Logger;
            Instance = this;

            Patch();*/
        }

        internal static void Patch()
        {
            harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            mls.LogDebug("Patching...");

            //harmony.PatchAll();
            harmony.PatchAll(typeof(Plugin));

            //all masked patches
            harmony.PatchAll(typeof(MaskedPlayerEnemyPatch));
            harmony.PatchAll(typeof(ShotgunItemPatch));
            harmony.PatchAll(typeof(GrabbableObjectPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));

            mls.LogDebug("Finished patching!");
        }


        private void Start()
        {
            if (enableMaskedFeatures)
            {
                mls.LogInfo((object)"Experimental feature has been enabled! Masked AI's behavior changes.");
            }
            else
            {
                mls.LogInfo((object)"Experimental feature has been disabled! This does not change the behavior of the Masked AI.");
            }
            if (Chainloader.PluginInfos.Keys.Any((string k) => k == "RugbugRedfern.SkinwalkerMod") && enableSkinWalkers)
            {
                mls.LogInfo((object)logPluginName + " <-> SkinWalkers Integrated!");
                skinWalkersIntegrated = true;
            }
            if (Chainloader.PluginInfos.Keys.Any((string w) => w == "Tim_Shaw.Wendigos_Voice_Cloning") && enableWendigos)
            {
                mls.LogInfo((object)logPluginName + " <-> Wendigos_Voice_Cloning Integrated!");
                wendigosIntegrated = true;

            }
        }

        private void LoadAssets()
        {
            try
            {
                Bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(PluginDirectory), "mapdotanimpack"));
            }
            catch (Exception ex)
            {
                mls.LogError((object)("Couldn't load asset bundle: " + ex.Message));
                return;
            }
            try
            {
                MapDotRework = Bundle.LoadAsset<RuntimeAnimatorController>("MapDotRework.controller");
                MapDotPrefab = Bundle.LoadAsset<GameObject>("MaskedMapDot.prefab");
                MaskedAnimController = Bundle.LoadAsset<RuntimeAnimatorController>("MaskedMetarig.controller");
                this.Logger.LogInfo((object)"Successfully loaded assets!");
            }
            catch (Exception ex2)
            {
                this.Logger.LogError((object)("Couldn't load assets: " + ex2.Message));
            }
        }
    }
}