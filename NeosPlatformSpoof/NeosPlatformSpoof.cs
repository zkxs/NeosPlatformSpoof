using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Reflection;

namespace NeosPlatformSpoof
{
    public class NeosPlatformSpoof : NeosMod
    {
        public override string Name => "NeosPlatformSpoof";
        public override string Author => "runtime";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/zkxs/NeosPlatformSpoof";

        private static readonly Platform TARGET_PLATFORM = Platform.Android;

        private static FieldInfo _userInitializingEnabled;

        public override void OnEngineInit()
        {
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
                if (message is ControlMessage controlMessage && controlMessage.ControlMessageType == ControlMessage.Message.JoinRequest)
                {
                    Platform oldPlatform = Platform.Other;
                    if (controlMessage.Data.TryExtract("Platform", ref oldPlatform))
                    {
                        controlMessage.Data.AddOrUpdate("Platform", TARGET_PLATFORM);
                        Msg($"spoofed join Platform from {oldPlatform} to {TARGET_PLATFORM}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(World), nameof(World.CreateHostUser))]
        private static class HostPatch
        {
            private static void Postfix(ref User __result)
            {
                Platform oldPlatform = __result.Platform;
                bool oldInitializingEnabled = (bool) _userInitializingEnabled.GetValue(__result);
                _userInitializingEnabled.SetValue(__result, true);
                __result.Platform = TARGET_PLATFORM;
                _userInitializingEnabled.SetValue(__result, oldInitializingEnabled);
                Msg($"spoofed host platform from {oldPlatform} to {__result.Platform}");
            }
        }
    }
}
