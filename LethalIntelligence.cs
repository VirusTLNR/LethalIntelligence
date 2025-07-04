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
    [BepInDependency("LethalNetworkAPI", BepInDependency.DependencyFlags.HardDependency)]
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
        public static AssetBundle MapDotBundle, MaskedAnimationsBundle;

        //config settings
            //gen
        public static bool enableMaskedFeatures, enableSkinWalkers, enableWendigos, enableMirage;

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
        public static bool mirageIntegrated;
        public static bool alwaysHearActiveWalkiesIntegrated;
        //public static bool moreEmotesIntegrated; //not currently implemented?

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
            mls.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} has loaded!");

            //loading config
            //general settings
            enableMaskedFeatures = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Masked AI Features", true, "Turn on masked AI features. If this feature is disabled, it will only change Masked's radar movement. If all masked Personalities are disabled, this will be disabled by default. *This option must be enabled to change Masked's AI.*").Value;
            enableSkinWalkers = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "SkinWalkers mod Compatibility", true, "Enables compatibility with the SkinWalkers mod. (Requires SkinWalkers mod installed, automatically disables on launch if not installed)").Value;
            enableWendigos = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Wendigos mod Compatibility", true, "Enables compatibility with the Wendigos_Voice_Cloning mod. (Requires Wendigos_Voice_Cloning mod installed, automatically disables on launch if not installed)").Value;
            enableMirage = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Mirage mod Compatibility", true, "Enables compatibility with the Mirage mod. (Requires Mirage mod installed, automatically disables on launch if not installed)").Value;

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
            debugModeSetting = ((BaseUnityPlugin)this).Config.Bind<bool>("DebugMode", "Debug Mode", true, "Enables more spammy logs for debugging, will be enabled automatically if imperium is installed. (all other DebugMode settings are ignored if Debug Mode is disabled)").Value;
            debugStatusDelay = ((BaseUnityPlugin)this).Config.Bind<int>("DebugMode", "Status Report Delay", 0, "How often should status reports (only updates when information changes) be logged (higher number = less log spam but also less accurate as not all information is gathered").Value;
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
            RemoveOrphanedConfigs();
            Patch();
            /*Logger = base.Logger;
            Instance = this;
            Patch();*/
            Start(); //because it seemingly doesnt want to run.
        }

        internal void RemoveOrphanedConfigs()
        {
            //for testing that orphan removel is working.
            //int orphan = ((BaseUnityPlugin)this).Config.Bind<int>("Test", "Orphan", 100, "just an orphan").Value;

            PropertyInfo orphanedEntriesProp = ((BaseUnityPlugin)this).Config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

            var orphanedEntries = (Dictionary<BepInEx.Configuration.ConfigDefinition, string>)orphanedEntriesProp.GetValue(Config, null);
            if(orphanedEntries.Count !=0)
            {
                Plugin.mls.LogInfo("Found Orphaned Config Entries - Removing them all as they are not needed anymore");
            }

            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            ((BaseUnityPlugin)this).Config.Save(); // Save the config file
        }

        internal static void Patch()
        {
            harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            mls.LogDebug("Patching...");

            //harmony.PatchAll();
            harmony.PatchAll(typeof(Plugin));

            //all masked patches
            harmony.PatchAll(typeof(MaskedPlayerEnemyPatch));
            //harmony.PatchAll(typeof(MaskedAIRevamp));
            harmony.PatchAll(typeof(ShotgunItemPatch));
            harmony.PatchAll(typeof(GrabbableObjectPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));

            mls.LogDebug("Finished patching!");
        }


        private void Start()
        {
            //calling this yet again in awake.. because sometimes it doesnt run
            if (enableMaskedFeatures)
            {
                mls.LogInfo((object)"MaskedPersonalities feature has been enabled! Masked AI's behavior is now modified.");
            }
            else
            {
                mls.LogInfo((object)"MaskedPersonalities feature has been disabled! Masked AI's behaviour will not be modified.");
            }
            if (Chainloader.PluginInfos.Keys.Any((string k) => k == "RugbugRedfern.SkinwalkerMod") && enableSkinWalkers && enableMaskedFeatures)
            {
                mls.LogInfo((object)logPluginName + " <-> SkinWalkers Integrated!");
                skinWalkersIntegrated = true;
            }
            if (Chainloader.PluginInfos.Keys.Any((string w) => w == "Tim_Shaw.Wendigos_Voice_Cloning") && enableWendigos && enableMaskedFeatures)
            {
                mls.LogInfo((object)logPluginName + " <-> Wendigos_Voice_Cloning Integrated!");
                wendigosIntegrated = true;
            }
            if (Chainloader.PluginInfos.Keys.Any((string m) => m == "qwbarch.Mirage") && enableMirage && enableMaskedFeatures)
            {
                mls.LogInfo((object)logPluginName + " <-> Mirage Integrated!");
                mirageIntegrated = true;
            }
            if (Chainloader.PluginInfos.Keys.Any((string s) => s == "suskitech.LCAlwaysHearActiveWalkie") && enableMaskedFeatures)
            {
                mls.LogInfo((object)logPluginName + " <-> AlwaysHearActiveWalkies Support Enabled!");
                alwaysHearActiveWalkiesIntegrated = true;
            }
            mls.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} finished checking for available dependencies!");
        }

        private void LoadAssets()
        {
            //load mapdot
            try
            {
                MapDotBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(PluginDirectory), "mapdot.bundle"));
            }
            catch (Exception ex)
            {
                mls.LogError((object)("Couldn't load Mapdot.bundle: " + ex.Message));
                return;
            }
            try
            {
                MapDotRework = MapDotBundle.LoadAsset<RuntimeAnimatorController>("MapDotRework.controller");
                MapDotPrefab = MapDotBundle.LoadAsset<GameObject>("MaskedMapDot.prefab");
                this.Logger.LogInfo((object)"Successfully loaded MapDot assets!");
            }
            catch (Exception ex2)
            {
                this.Logger.LogError((object)("Couldn't load MapDot assets: " + ex2.Message));
            }
            MapDotBundle.Unload(false);

            //load masked animations
            try
            {
                MaskedAnimationsBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(PluginDirectory), "maskedanimations.bundle"));
                /*foreach (var animation in MaskedAnimationsBundle.GetAllAssetNames())
                {
                    Plugin.mls.LogError(animation.ToString());
                }*/
            }
            catch (Exception ex)
            {
                mls.LogError((object)("Couldn't load MaskedAnimations.bundle: " + ex.Message));
                return;
            }
            try
            {
                MaskedAnimController = MaskedAnimationsBundle.LoadAsset<RuntimeAnimatorController>("MaskedMetarig.controller");
                this.Logger.LogInfo((object)"Successfully loaded MaskedAnimations assets!");
            }
            catch (Exception ex2)
            {
                this.Logger.LogError((object)("Couldn't load MaskedAnimations assets: " + ex2.Message));
            }
            MaskedAnimationsBundle.Unload(false);
        }
    }
}