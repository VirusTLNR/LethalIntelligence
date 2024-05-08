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

namespace LethalIntelligence
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    [LobbyCompatibility(CompatibilityLevel.Everyone, VersionStrictness.Patch)]
    public class LethalIntelligence : BaseUnityPlugin
    {
        public static LethalIntelligence Instance { get; private set; } = null!;
        internal new static ManualLogSource mls { get; private set; } = null!;
        internal static Harmony? harmony { get; set; }

        //variables for mod
        //====================
        public static string logPluginName = "Lethal Intelligence";
        public static AssetBundle Bundle;
        public static bool enableExperimentalFeatures;
        public static bool enableSkinWalkers;
        public static bool useTerminal;
        public static bool useTerminalCredit;
        public static bool maskedShipDeparture;

        public static GameObject MapDotPrefab;
        public static RuntimeAnimatorController MaskedAnimController;
        public static RuntimeAnimatorController MapDotRework;
        public static string PluginDirectory;
        public static bool skinWalkersIntergrated;
        public static bool moreEmotesIntergrated;
        //end of variables for mod

        private void Awake()
        {
            if ((Object)(object)Instance == (Object)null)
            {
                Instance = this;
            }
            mls = base.Logger;
            PluginDirectory = ((BaseUnityPlugin)this).Info.Location;
            LoadAssets();
            mls.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
            enableExperimentalFeatures = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "Experimental Features", true, "Turn on experimental features. If this feature is disabled, it will only change Masked's radar movement. *This option must be enabled to change Masked's AI.*").Value;
            enableSkinWalkers = ((BaseUnityPlugin)this).Config.Bind<bool>("General", "SkinWalkers mod Compatibility", true, "Enables compatibility with the SkinWalkers mod. (Requires SkinWalkers mod installed, automatically disables on launch if not installed)").Value;
            useTerminal = ((BaseUnityPlugin)this).Config.Bind<bool>("Masked", "Masked terminal access", true, "Allows Masked to use the terminal.").Value;
            useTerminalCredit = ((BaseUnityPlugin)this).Config.Bind<bool>("Masked", "Masked uses credits", false, "(Not working rn) Allows Masked to use the terminal to spend credits.").Value;
            maskedShipDeparture = ((BaseUnityPlugin)this).Config.Bind<bool>("Masked", "Masked pulls the brake lever", false, "(Not working rn) Allows Masked to pull the brake lever. Um... really...?").Value;

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
            harmony.PatchAll(typeof(LethalIntelligence));
            harmony.PatchAll(typeof(MaskedPlayerEnemyPatch));
            harmony.PatchAll(typeof(ShotgunItemPatch));
            harmony.PatchAll(typeof(GrabbableObjectPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));

            mls.LogDebug("Finished patching!");
        }


        private void Start()
        {
            if (enableExperimentalFeatures)
            {
                mls.LogInfo((object)"Experimental feature has been enabled! Masked AI's behavior changes.");
            }
            else
            {
                mls.LogInfo((object)"Experimental feature has been disabled! This does not change the behavior of the Masked AI.");
            }
            if (Chainloader.PluginInfos.Keys.Any((string k) => k == "RugbugRedfern.SkinwalkerMod"))
            {
                mls.LogInfo((object)logPluginName + " <-> SkinWalker Intergrated!");
                skinWalkersIntergrated = true;
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