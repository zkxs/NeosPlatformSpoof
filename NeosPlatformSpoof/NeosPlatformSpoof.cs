using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NeosPlatformSpoof
{
    public class NeosPlatformSpoof : NeosMod
    {
        public override string Name => "NeosPlatformSpoof";
        public override string Author => "runtime";
        public override string Version => "1.1.0";
        public override string Link => "https://github.com/zkxs/NeosPlatformSpoof";

        private static FieldInfo _userInitializingEnabled;
        private static ModConfiguration _config;

        private static readonly ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabled", "Enable platform spoofing", () => true);
        private static readonly ModConfigurationKey<Platform> TARGET_PLATFORM = new ModConfigurationKey<Platform>("spoofed platform", "This will appear as your platform in new sessions you join.", () => Platform.Android);

        public override ModConfigurationDefinition GetConfigurationDefinition()
        {
            HashSet<ModConfigurationKey> keys = new HashSet<ModConfigurationKey>();
            keys.Add(ENABLED);
            keys.Add(TARGET_PLATFORM);
            return DefineConfiguration(new Version(1, 0, 0), keys);
        }

        public override void OnEngineInit()
        {
            _config = GetConfiguration();
            Harmony harmony = new Harmony("net.michaelripley.NeosPlatformSpoof");

            _userInitializingEnabled = AccessTools.DeclaredField(typeof(User), "InitializingEnabled");
            if (_userInitializingEnabled == null)
            {
                Error("Could not reflect field User.InitializingEnabled");
                return;
            }

            harmony.PatchAll();
            Msg("Hooks installed successfully!");
        }

        [HarmonyPatch(typeof(Session), nameof(Session.EnqueueForTransmission), new Type[] { typeof(SyncMessage) })]
        private static class ClientPatch
        {
            private static void Prefix(ref SyncMessage message)
            {
                if (_config.GetValue(ENABLED) && message is ControlMessage controlMessage && controlMessage.ControlMessageType == ControlMessage.Message.JoinRequest)
                {
                    Platform oldPlatform = Platform.Other;
                    if (controlMessage.Data.TryExtract("Platform", ref oldPlatform))
                    {
                        Platform newPlatform = _config.GetValue(TARGET_PLATFORM);
                        controlMessage.Data.AddOrUpdate("Platform", newPlatform);
                        Msg($"spoofed join Platform from {oldPlatform} to {newPlatform}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(World), nameof(World.CreateHostUser))]
        private static class HostPatch
        {
            private static void Postfix(ref User __result)
            {
                if (_config.GetValue(ENABLED))
                {
                    Platform oldPlatform = __result.Platform;
                    bool oldInitializingEnabled = (bool)_userInitializingEnabled.GetValue(__result);
                    _userInitializingEnabled.SetValue(__result, true);
                    __result.Platform = _config.GetValue(TARGET_PLATFORM);
                    _userInitializingEnabled.SetValue(__result, oldInitializingEnabled);
                    Msg($"spoofed host platform from {oldPlatform} to {__result.Platform}");
                }
            }
        }
    }
}
