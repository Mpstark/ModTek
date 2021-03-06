using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using Harmony;
using static ModTek.Features.Logging.MTLogger;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.CustomStreamingAssets.Patches
{
    /// <summary>
    /// Patch the GameTipList to use modded tip list if existing.
    /// </summary>
    [HarmonyPatch(typeof(DebugSettings), nameof(DebugSettings.Load))]
    internal static class DebugSettings_Load_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                    AccessTools.Method(typeof(TextReader), nameof(TextReader.ReadToEnd)),
                    AccessTools.Method(typeof(DebugSettings_Load_Patch), nameof(ReadToEnd))
                );
        }

        public static string ReadToEnd(this StreamReader reader)
        {
            try
            {
                return CustomStreamingAssetsFeature.GetDebugSettings();
            }
            catch (Exception e)
            {
                Log("Error trying to read custom debug settings", e);
                return reader.ReadToEnd();
            }
        }
    }
}
