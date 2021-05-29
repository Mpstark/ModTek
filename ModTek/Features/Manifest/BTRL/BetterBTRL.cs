using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ModTek.Features.Manifest.Mods;
using ModTek.Util;
using static ModTek.Logging.Logger;

namespace ModTek.Features.Manifest.BTRL
{
    internal class BetterBTRL
    {
        public static readonly BetterBTRL Instance = new();

        private ContentPackIndex packIndex;
        private readonly TypedManifest currentManifest = new();

        private readonly VersionManifest defaultManifest;
        private readonly List<VersionManifestAddendum> hbsAddendums = new();
        private readonly List<ModAddendumManifest> orderedModAddendumManifests = new();
        private readonly Dictionary<string, List<VersionManifestEntry>> addendumEntryOverrides = new();

        private bool HasChanges;

        internal void TryFinalizeDataLoad(ContentPackIndex contentPackIndex)
        {
            SetContentPackIndex(contentPackIndex);
            if (contentPackIndex.AllContentPacksLoaded())
            {
                currentManifest.DumpToDisk();
                Log("Owned content packs: " + packIndex?.GetOwnedContentPacks().Aggregate((a, b) => $"{a} {b}"));
                Log("HBS Addendums: " + hbsAddendums.Select(x => x.Name).Aggregate((a, b) => $"{a} {b}"));

                Log("Mod Addendums:");
                foreach (var modAddendum in orderedModAddendumManifests)
                {
                    string requires;
                    if (modAddendum.RequiredAddendums == null || modAddendum.RequiredAddendums.Length == 0)
                    {
                        requires = "";
                    }
                    else
                    {
                        requires = " requires: " + modAddendum.RequiredAddendums.Aggregate((a, b) => $"{a} {b}");
                    }
                    Log($"\t{modAddendum.Addendum.Name}{requires}");
                }
                ModsManifest.BTRLContentPackLoaded();
            }
        }

        internal void AddAddendumOverrideEntry(string addendumName, VersionManifestEntry manifestEntry)
        {
            addendumEntryOverrides.GetOrCreate(addendumName).Add(manifestEntry);
            HasChanges = true;
        }

        private VersionManifestAddendum ApplyOverrides(VersionManifestAddendum addendum)
        {
            if (!addendumEntryOverrides.TryGetValue(addendum.Name, out var overrides))
            {
                return addendum;
            }

            var copy = new VersionManifestAddendum(addendum.Name);
            copy.AddRange(addendum.Entries);
            copy.AddRange(overrides); // duplicates are sorted inside TypedManifest
            return copy;
        }

        // methods order is same as dnSpy lists them

        public void SetContentPackIndex(ContentPackIndex contentPackIndex)
        {
            packIndex = contentPackIndex;
            HasChanges = true;
            RefreshTypedEntries();
        }

        public void ApplyAddendum(VersionManifestAddendum addendum)
        {
            if (hbsAddendums.Any(x => addendum.Name.Equals(x.Name)))
            {
                return;
            }
            hbsAddendums.Add(addendum);
            HasChanges = true;
            RefreshTypedEntries();
        }

        public void AddModAddendum(ModAddendumManifest modManifest)
        {
            orderedModAddendumManifests.Add(modManifest);
            HasChanges = true;
        }

        public void RemoveAddendum(VersionManifestAddendum addendum)
        {
            hbsAddendums.RemoveAll(x => addendum.Name.Equals(x.Name));
            HasChanges = true;
            RefreshTypedEntries();
        }

        public VersionManifestAddendum GetAddendumByName(string name)
        {
            return hbsAddendums.FirstOrDefault(x => x.Name == name);
        }

        #region memory stores

        // this region is copy pasted from original and kept same except for some calls to Manifest and BTRL itself

        private Dictionary<string, VersionManifestMemoryStore> memoryStores = new();
        private Dictionary<BattleTechResourceType, Dictionary<string, List<VersionManifestMemoryStore>>> memoryStoreResourceIndex = new();

        public void ApplyMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            if (memoryStores.ContainsKey(memoryStore.Name))
            {
                return;
            }

            ApplyAddendum(memoryStore);
            memoryStores.Add(memoryStore.Name, memoryStore);
            memoryStore.SubscribeToContentsChanged(IndexMemoryStore);
            IndexMemoryStore(memoryStore);
            RefreshTypedEntries();
        }

        public void RemoveMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            if (!memoryStores.ContainsKey(memoryStore.Name))
            {
                return;
            }

            RemoveAddendum(memoryStore);
            memoryStores.Remove(memoryStore.Name);
            memoryStore.SubscribeToContentsChanged(IndexMemoryStore);
            UnIndexMemoryStore(memoryStore);
            RefreshTypedEntries();
        }

        private void IndexMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            UnIndexMemoryStore(memoryStore);
            foreach (VersionManifestEntry versionManifestEntry in memoryStore)
            {
                var key = versionManifestEntry.Type.FromString();
                if (!memoryStoreResourceIndex.TryGetValue(key, out var dictionary))
                {
                    dictionary = new Dictionary<string, List<VersionManifestMemoryStore>>();
                    memoryStoreResourceIndex[key] = dictionary;
                }

                if (!dictionary.TryGetValue(versionManifestEntry.Id, out var list))
                {
                    list = new List<VersionManifestMemoryStore>();
                    dictionary[versionManifestEntry.Id] = list;
                }

                if (!list.Contains(memoryStore))
                {
                    list.Add(memoryStore);
                }
            }
        }

        private bool UnIndexMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            foreach (var obj in memoryStore)
            {
                var versionManifestEntry = (VersionManifestEntry) obj;
                var key = versionManifestEntry.Type.FromString();
                Dictionary<string, List<VersionManifestMemoryStore>> dictionary;
                List<VersionManifestMemoryStore> list;
                if (memoryStoreResourceIndex.TryGetValue(key, out dictionary) && dictionary.TryGetValue(versionManifestEntry.Id, out list))
                {
                    return list.Remove(memoryStore);
                }
            }

            return false;
        }

        public List<VersionManifestMemoryStore> GetMemoryStoresContainingEntry(BattleTechResourceType resourceType, string id)
        {
            Dictionary<string, List<VersionManifestMemoryStore>> dictionary;
            if (!memoryStoreResourceIndex.TryGetValue(resourceType, out dictionary))
            {
                return null;
            }

            List<VersionManifestMemoryStore> result;
            if (!dictionary.TryGetValue(id, out result))
            {
                return null;
            }

            return result;
        }

        public VersionManifestMemoryStore GetMemoryStoreByName(string name)
        {
            VersionManifestMemoryStore result;
            if (!memoryStores.TryGetValue(name, out result))
            {
                return null;
            }

            return result;
        }

        #endregion

        private BetterBTRL()
        {
            defaultManifest = VersionManifestUtilities.ManifestFromCSV(VersionManifestUtilities.MANIFEST_FILEPATH);
            SetContentPackIndex(UnityGameInstance.BattleTechGame?.DataManager?.ContentPackIndex);
        }

        private Stopwatch sw = new();
        internal void RefreshTypedEntries() // this is called way too often in vanilla cases, but not sure what depends on this
        {
            if (!HasChanges) // it changes all the time anyway
            {
                return;
            }

            sw.Start();
            currentManifest.Reset(defaultManifest.Entries, packIndex);
            var activeAndOwnedAddendums = new List<string>();

            foreach (var addendum in hbsAddendums)
            {
                // if content pack index not yet loaded, assume its owned (vanilla behavior)
                var isOwned = packIndex?.IsContentPackOwned(addendum.Name) ?? true;
                if (isOwned)
                {
                    activeAndOwnedAddendums.Add(addendum.Name);
                }

                currentManifest.AddAddendum(ApplyOverrides(addendum));
            }

            foreach (var modAddendum in orderedModAddendumManifests)
            {
                if (modAddendum.RequiredAddendums != null && modAddendum.RequiredAddendums.Except(activeAndOwnedAddendums).Any())
                {
                    // skip since not all requirements are met
                    continue;
                }

                currentManifest.AddAddendum(ApplyOverrides(modAddendum.Addendum));
            }
            sw.Stop();
            LogIf(sw.ElapsedMilliseconds > 500, $"RefreshTypedEntries Total {sw.Elapsed}s");
        }

        public VersionManifestEntry[] AllEntries()
        {
            return currentManifest.AllEntries(false);
        }

        public VersionManifestEntry[] AllEntriesOfResource(BattleTechResourceType type, bool filterByOwnership = false)
        {
            return currentManifest.AllEntriesOfResource(type, filterByOwnership);
        }

        public VersionManifestEntry[] AllEntriesOfResourceFromAddendum(BattleTechResourceType type, VersionManifestAddendum addendum, bool filterByOwnership = false)
        {
            return addendum.Entries.Where(x => x.Type == type.ToString() && (!filterByOwnership || packIndex.IsResourceOwned(x.Id))).ToArray();
        }

        public VersionManifestEntry EntryByID(string id, BattleTechResourceType type, bool filterByOwnership = false)
        {
            return currentManifest.GetEntryByID(id, type, filterByOwnership);
        }

        public void RemoveEntry(VersionManifestEntry entry)
        {
            // only used by ModLoader, which we disable anyway
            // we dont support removal, since we cannot remove base types from MDDB anyway
            throw new NotImplementedException();
        }
    }
}