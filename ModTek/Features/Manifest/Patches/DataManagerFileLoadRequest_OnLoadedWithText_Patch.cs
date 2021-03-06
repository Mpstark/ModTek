﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch]
    public static class DataManagerFileLoadRequest_OnLoadedWithText_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static IEnumerable<MethodBase> GetOnLoadedWithTextMethods()
        {
            var parent = typeof(DataManager.FileLoadRequest);
            foreach (var subclass in parent.Assembly.GetTypes().Where(type => type.IsSubclassOf(parent)))
            {
                if (subclass.IsAbstract)
                {
                    continue;
                }

                var method = AccessTools.DeclaredMethod(subclass, "OnLoadedWithText");
                if (method == null)
                {
                    continue;
                }

                yield return method;
            }
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            return GetOnLoadedWithTextMethods();
        }

        [HarmonyPriority(Priority.High)]
        public static void Prefix(VersionManifestEntry ___manifestEntry, ref string text)
        {
            ModsManifest.MergeContentIfApplicable(___manifestEntry, ref text);
        }
    }
}
