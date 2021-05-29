﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.Mods;
using ModTek.Misc;
using static ModTek.Logging.Logger;
using CustomResourcesDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, BattleTech.VersionManifestEntry>>;

namespace ModTek.Features.CustomResources
{
    internal static class CustomResourcesFeature
    {
        private static readonly CustomResourcesDict CustomResources = new();

        private static string DefaultDebugSettingsPath;

        internal static void Setup()
        {
            // setup custom resources for ModTek types with fake VersionManifestEntries
            CustomResources.Add(BTConstants.CustomType_Video, new Dictionary<string, VersionManifestEntry>());
            CustomResources.Add(BTConstants.CustomType_SoundBank, new Dictionary<string, VersionManifestEntry>());

            CustomResources.Add(BTConstants.CustomType_DebugSettings, new Dictionary<string, VersionManifestEntry>());
            DefaultDebugSettingsPath = Path.Combine(FilePaths.StreamingAssetsDirectory, FilePaths.DebugSettingsPath);
            CustomResources[BTConstants.CustomType_DebugSettings]["settings"] = new VersionManifestEntry(
                "settings",
                DefaultDebugSettingsPath,
                "DebugSettings",
                DateTime.Now,
                "1"
            );

            CustomResources.Add(BTConstants.CustomType_GameTip, new Dictionary<string, VersionManifestEntry>());
            CustomResources[BTConstants.CustomType_GameTip]["general"] = new VersionManifestEntry(
                "general",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "general.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources[BTConstants.CustomType_GameTip]["combat"] = new VersionManifestEntry(
                "combat",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "combat.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources[BTConstants.CustomType_GameTip]["lore"] = new VersionManifestEntry(
                "lore",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "lore.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources[BTConstants.CustomType_GameTip]["sim"] = new VersionManifestEntry(
                "sim",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "sim.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
        }

        internal static bool Add(ModEntry entry)
        {
            if (entry.Type == null || !CustomResources.ContainsKey(entry.Type))
            {
                return false;
            }
            CustomResources[entry.Type][entry.Id] = entry.CreateVersionManifestEntry();
            return true;
        }

        internal static void ProcessModDef(ModDefEx modDef)
        {
            foreach (var customResourceType in modDef.CustomResourceTypes)
            {
                if (BTConstants.VANILLA_TYPES.Contains(customResourceType) || BTConstants.MODTEK_TYPES.Contains(customResourceType))
                {
                    Log($"\tWarning: {modDef.Name} has a custom resource type that has the same name as a vanilla/modtek resource type. Ignoring this type.");
                    continue;
                }

                if (!CustomResources.ContainsKey(customResourceType))
                {
                    CustomResources.Add(customResourceType, new Dictionary<string, VersionManifestEntry>());
                }
            }
        }

        internal static void FinalizeResourceLoading()
        {
            if (CustomResources[BTConstants.CustomType_DebugSettings]["settings"].FilePath != DefaultDebugSettingsPath)
            {
                DebugBridge.LoadSettings(CustomResources[BTConstants.CustomType_DebugSettings]["settings"].FilePath);
            }
        }

        internal static CustomResourcesDict GetCopyOfResourceForType(HashSet<string> modDefCustomResourceTypes)
        {
            var customResources = new CustomResourcesDict();
            foreach (var resourceType in modDefCustomResourceTypes)
            {
                customResources.Add(resourceType, new Dictionary<string, VersionManifestEntry>(CustomResources[resourceType]));
            }
            return customResources;
        }

        internal static VersionManifestEntry GetGameTip(string filename)
        {
            return CustomResources[BTConstants.CustomType_GameTip].Values.LastOrDefault(entry => entry.Id == Path.GetFileNameWithoutExtension(filename));
        }

        public static VersionManifestEntry GetVideo(string videoName)
        {
            return CustomResources[BTConstants.CustomType_Video].Values.LastOrDefault(entry => entry.Id == videoName || entry.Id == Path.GetFileNameWithoutExtension(videoName));
        }

        public static VersionManifestEntry GetSoundBank(string name)
        {
            return CustomResources[BTConstants.CustomType_SoundBank].TryGetValue(name, out var entry) ? entry : null;
        }
    }
}
