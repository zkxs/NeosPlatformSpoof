using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;

namespace NeosPlatformSpoof
{
    public class NeosPlatformSpoof : NeosMod
    {
        public override string Name => "NeosPlatformSpoof";
        public override string Author => "runtime";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/zkxs/NeosPlatformSpoof";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.michaelripley.NeosPlatformSpoof");
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
                        controlMessage.Data.AddOrUpdate("Platform", Platform.Android);
                        Msg($"spoofed join Platform from {oldPlatform} to {Platform.Android}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(World), nameof(World.CreateHostUser))]
        private static class HostPatch
        {
            private static void Postfix(ref User __result)
            {
                Msg($"spoofed host platform from {__result.Platform} to {Platform.Android}");
                __result.Platform = Platform.Android;
            }
        }
    }
}
