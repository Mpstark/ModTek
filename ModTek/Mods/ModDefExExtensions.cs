using System.Collections.Generic;
using ModTek.Util;

namespace ModTek.Mods
{
    internal static class ModDefExExtensions
    {
        public static void GatherAffectingOfflineRec(this ModDefEx mod)
        {
            Dictionary<ModDefEx, bool> deps = new Dictionary<ModDefEx, bool>();
            Logger.Log("Gathering " + mod.Name + "->Disable influence. My state:" + mod.Enabled + " fail:" + (mod.LoadFail ? mod.FailReason : "no"));
            GatherAffectingOfflineRec(mod, ref deps, 1);
            mod.AffectingOffline = deps;
        }

        private static void GatherAffectingOfflineRec(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps, int level)
        {
            foreach (var dmod in mod.DependsOnMe)
            {
                if (deps.ContainsKey(dmod) == false)
                {
                    string i = new string(' ', level);
                    Logger.Log(i + dmod.Name + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                    deps.Add(dmod, false);
                    GatherAffectingOfflineRec(dmod, ref deps, level + 1);
                };
            }
        }

        private static void GatherAffectingOnlineRec(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps, int level)
        {
            foreach (string dep in mod.DependsOn)
            {
                string i = new string(' ', level);
                if (ModTek.allModDefs.ContainsKey(dep) == false)
                {
                    Logger.Log(i + dep + " state:Absent!");
                    continue;
                }
                ModDefEx dmod = ModTek.allModDefs[dep];
                if (deps.ContainsKey(dmod) == false)
                {
                    Logger.Log(i + dmod.Name + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                    deps.Add(dmod, true);
                    GatherAffectingOnlineRec(dmod, ref deps, level + 1);
                }
            }
        }

        private static void GatherConflicts(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps)
        {
            foreach (string dep in mod.ConflictsWith)
            {
                if (ModTek.allModDefs.ContainsKey(dep) == false)
                {
                    Logger.Log("  due to " + mod.Name + " with " + dep + " state:Abcent");
                    continue;
                }
                ModDefEx dmod = ModTek.allModDefs[dep];
                Logger.Log("  due to " + mod.Name + " with " + dmod.Name + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                if (deps.ContainsKey(dmod) == false)
                {
                    deps.Add(dmod, false);
                }
            }
        }

        public static void GatherAffectingOnline(this ModDefEx mod)
        {
            Dictionary<ModDefEx, bool> deps = new Dictionary<ModDefEx, bool>();
            Logger.Log("Gathering " + mod.Name + "->Enable influence. My state:" + mod.Enabled + " fail:" + (mod.LoadFail ? mod.FailReason : "no"));
            Logger.Log((string) " I'm depends on:");
            GatherAffectingOnlineRec(mod, ref deps, 2);
            HashSet<ModDefEx> conflicts = deps.Keys.ToHashSet();
            Logger.Log((string) " Conflicts:");
            foreach (ModDefEx cmod in conflicts)
            {
                GatherConflicts(cmod, ref deps);
            }
            mod.AffectingOnline = deps;
        }
    }
}
