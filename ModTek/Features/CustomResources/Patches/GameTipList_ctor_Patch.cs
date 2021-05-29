using System.Collections.Generic;
using System.IO;
using BattleTech;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.CustomResources.Patches
{
    /// <summary>
    /// Patch the GameTipList to use modded tip list if existing.
    /// </summary>
    [HarmonyPatch(typeof(GameTipList), MethodType.Constructor, typeof(string), typeof(int))]
    internal static class GameTipList_ctor_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(string filename, List<string> ___tips)
        {
            var tipEntry = CustomResourcesFeature.GetGameTip(filename);
            if (tipEntry == null)
            {
                return;
            }

            ___tips.Clear();

            var text = File.ReadAllText(tipEntry.FilePath);
            foreach (var tip in text.Split('\n'))
            {
                var trimmedTip = tip.Trim();
                if (!string.IsNullOrEmpty(trimmedTip))
                {
                    ___tips.Add(trimmedTip);
                }
            }
        }
    }
}