﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.PackageManager;
using UnityEngine;

namespace AssetInventory
{
    public sealed class PackageImporter : AssertImporter
    {
        private const float MAX_META_DATA_WAIT_TIME = 30f;
        private const int BREAK_INTERVAL = 50;

        public async Task IndexRough(string path, bool fromAssetStore)
        {
            ResetState(false);

            // pass 1: find latest cached packages
            int progressId = MetaProgress.Start("Discovering packages");
            string[] packages = Directory.GetFiles(path, "package.json", SearchOption.AllDirectories);
            MainCount = packages.Length;
            bool tagsChanged = false;
            for (int i = 0; i < packages.Length; i++)
            {
                if (CancellationRequested) break;

                string package = packages[i];
                MetaProgress.Report(progressId, i + 1, packages.Length, Path.GetFileName(Path.GetDirectoryName(package)));
                if (i % BREAK_INTERVAL == 0) await Task.Yield(); // let editor breath

                Package info;
                try
                {
                    info = JsonConvert.DeserializeObject<Package>(File.ReadAllText(package), new JsonSerializerSettings
                    {
                        Error = (sender, error) =>
                        {
                            Debug.Log($"Field inside package manifest '{package}' is malformed. This data will be ignored: {error.ErrorContext.Path}");
                            error.ErrorContext.Handled = true;
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Package manifest inside '{package}' is malformed and could not be read: {e.Message}");
                    continue;
                }
                if (info == null)
                {
                    Debug.LogError($"Could not read package manifest: {package}");
                    continue;
                }

                // create asset
                Asset asset = new Asset(info);

                // skip unchanged or older 
                Asset existing = Fetch(asset);
                if (existing != null)
                {
                    if (new SemVer(existing.Version) >= new SemVer(asset.Version)) continue;
                    asset = existing.CopyFrom(info);
                }
                else
                {
                    if (AssetInventory.Config.excludeByDefault) asset.Exclude = true;
                }

                asset.Location = Path.GetDirectoryName(package);
                asset.PackageSize = await IOUtils.GetFolderSize(asset.Location);

                // update progress only if really doing work to save refresh time in UI
                CurrentMain = $"{info.name} - {info.version}";
                MainProgress = i + 1;

                // handle tags
                if (AssetInventory.Config.importPackageKeywordsAsTags && info.keywords != null)
                {
                    foreach (string tag in info.keywords)
                    {
                        if (AssetInventory.AddTagAssignment(asset.Id, tag, TagAssignment.Target.Package, fromAssetStore)) tagsChanged = true;
                    }
                }

                // registry
                float maxWaitTime = Time.realtimeSinceStartup + MAX_META_DATA_WAIT_TIME;
                while (!AssetStore.IsMetadataAvailable() && Time.realtimeSinceStartup < maxWaitTime) await Task.Delay(25);
                if (AssetStore.IsMetadataAvailable())
                {
                    PackageInfo resolved = AssetStore.GetPackageInfo(info.name);
                    if (resolved != null) asset.CopyFrom(resolved);
                }

                asset.CurrentState = Asset.State.InProcess;
                UpdateOrInsert(asset);
            }
            MetaProgress.Remove(progressId);

            // pass 2: check for project packages which are not cached, e.g. git packages
            if (!CancellationRequested)
            {
                progressId = MetaProgress.Start("Discovering additional packages");
                List<PackageInfo> projectPackages = AssetStore.GetProjectPackages().ToList();
                MainCount = projectPackages.Count;
                for (int i = 0; i < projectPackages.Count; i++)
                {
                    if (CancellationRequested) break;

                    PackageInfo package = projectPackages[i];
                    if (package.source == PackageSource.BuiltIn) continue;

                    MetaProgress.Report(progressId, i + 1, packages.Length, package.name);
                    if (i % BREAK_INTERVAL == 0) await Task.Yield(); // let editor breath

                    // create asset
                    Asset asset = new Asset(package);

                    // skip unchanged or older 
                    Asset existing = Fetch(asset);
                    if (existing != null)
                    {
                        if (new SemVer(existing.Version) >= new SemVer(asset.Version)) continue;
                        asset = existing.CopyFrom(package);
                    }
                    else
                    {
                        if (AssetInventory.Config.excludeByDefault) asset.Exclude = true;
                    }

                    // update progress only if really doing work to save refresh time in UI
                    CurrentMain = $"{asset.SafeName} - {asset.Version}";
                    MainProgress = i + 1;

                    if (!string.IsNullOrWhiteSpace(asset.Location)) asset.PackageSize = await IOUtils.GetFolderSize(asset.Location);

                    asset.CurrentState = Asset.State.InProcess;
                    UpdateOrInsert(asset);
                }
                MetaProgress.Remove(progressId);
            }

            if (tagsChanged)
            {
                AssetInventory.LoadTags();
                AssetInventory.LoadTagAssignments();
            }

            MetaProgress.Remove(progressId);
            ResetState(true);
        }

        public async Task IndexDetails(int assetId = 0)
        {
            ResetState(false);

            FolderSpec importSpec = GetDefaultImportSpec();

            int progressId = MetaProgress.Start("Indexing packages");
            List<Asset> assets;
            if (assetId == 0)
            {
                assets = DBAdapter.DB.Table<Asset>().Where(a => a.AssetSource == Asset.Source.Package && a.CurrentState != Asset.State.Done).ToList();
            }
            else
            {
                assets = DBAdapter.DB.Table<Asset>().Where(a => a.Id == assetId && a.AssetSource == Asset.Source.Package).ToList();
            }
            MainCount = assets.Count;
            for (int i = 0; i < assets.Count; i++)
            {
                Asset asset = assets[i];
                if (CancellationRequested) break;

                if (AssetInventory.Config.indexPackageContents)
                {
                    CurrentMain = $"{asset.SafeName} - {asset.Version}";
                    MainProgress = i + 1;

                    // remove old files
                    DBAdapter.DB.Execute("delete from AssetFile where AssetId=?", asset.Id);

                    importSpec.location = asset.Location;
                    await new MediaImporter().Index(importSpec, asset, true, true);
                }

                asset.CurrentState = Asset.State.Done;
                Persist(asset);
            }
            MetaProgress.Remove(progressId);
            ResetState(true);
        }
    }
}