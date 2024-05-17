using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;
using System.Runtime.CompilerServices;

namespace LethalIntelligence.Patches
{
    public static class LobbyCompatibilityChecker
    {
        //lobby compatibility checker class created by mattymatty
        public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"); } }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            PluginHelper.RegisterPlugin(MyPluginInfo.PLUGIN_GUID, System.Version.Parse(MyPluginInfo.PLUGIN_VERSION), CompatibilityLevel.Everyone, VersionStrictness.Patch);

        }

    }
}
