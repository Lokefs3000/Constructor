using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using PrimaryEditor.Assets.Serialization;
using PrimaryEditor.Project;

namespace PrimaryEditor.Assets
{
    public sealed class AssetAssociator
    {
        private ConcurrentDictionary<AssetId, AssociationData> _assocations;

        internal AssetAssociator()
        {
            _assocations = new ConcurrentDictionary<AssetId, AssociationData>();
        }

        internal void LoadAssocationsFromDisk()
        {
            if (File.Exists(s_registryFile))
            {
                using Stream? inputStream = FileUtility.TryWaitOpenNoThrow(s_registryFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (inputStream == null)
                {
                    EdLog.Assets.Error("Failed to open file stream '{f}' for reading registry data", s_registryFile);
                    FileUtility.TryDelete(s_registryFile);
                    return;
                }

                using DataReader serializer = new DataReader(inputStream);

                if (serializer.ReadVersionHeader() != CurrentVersion)
                    throw new Exception("Invalid version in registry data");

                while (!serializer.IsAtEndOfStream)
                {
                    FileId fileId = (FileId)serializer.ReadGuid()!.Value;
                    int localId = serializer.ReadInt32()!.Value;

                    AssetId assetId = new AssetId(fileId, localId);
                    while (serializer.LeadingByte != '\n')
                    {
                        FileId localFileId = (FileId)serializer.ReadGuid()!.Value;
                        int localLocalId = serializer.ReadInt32()!.Value;

                        bool onlyOnReload = serializer.ReadBoolean()!.Value;

                        MakeAssociation(assetId, new AssetId(localFileId, localLocalId), actAsReloadFlag: onlyOnReload);
                    }

                    serializer.ReadNewLine();
                }
            }
        }

        internal void SaveAssociationsToDisk()
        {
            using Stream? outputStream = FileUtility.TryWaitOpenNoThrow(s_registryFile, FileMode.Create, FileAccess.Write, FileShare.None);
            if (outputStream == null)
            {
                EdLog.Assets.Error("Failed to open file stream '{f}' for writing registry data", s_registryFile);
                FileUtility.TryDelete(s_registryFile);
                return;
            }

            using DataWriter serializer = new DataWriter(outputStream);

            serializer.WriteVersionHeader(CurrentVersion);

            foreach (var (id, data) in _assocations)
            {
                using (data.Lock.EnterScope())
                {
                    if (data.Dependencies.Count > 0)
                    {
                        serializer.WriteValue(id.FileId);
                        serializer.WriteValue(id.LocalId);

                        foreach (AssetAssocationInfo dependency in data.Dependencies)
                        {
                            serializer.WriteValue(dependency.Target.FileId);
                            serializer.WriteValue(dependency.Target.LocalId);
                            serializer.WriteValue(dependency.OnlyOnReload);
                        }

                        serializer.FinishLine();
                    }
                }
            }
        }

        private void AddAssetAsDependent(AssetId id, AssetId newDependent)
        {
            if (id == newDependent)
                return;

            _assocations.AddOrUpdate(id, NewFactory, UpdateFactory);

            AssociationData NewFactory(AssetId _)
            {
                return new AssociationData(
                    new Lock(),
                    [],
                    [newDependent]);
            }

            AssociationData UpdateFactory(AssetId _, AssociationData associationData)
            {
                using (associationData.Lock.EnterScope())
                {
                    associationData.Dependents.Add(newDependent);
                }

                return associationData;
            }
        }

        private void RemoveAssetAsDependent(AssetId id, AssetId dependent)
        {
            if (id == dependent)
                return;

            if (_assocations.TryGetValue(id, out AssociationData associationData))
            {
                using (associationData.Lock.EnterScope())
                {
                    associationData.Dependents.Add(dependent);

                    if (associationData.Dependents.Count == 0 && associationData.Dependencies.Count == 0)
                        _assocations.TryRemove(id, out _);
                }
            }
        }

        public void MakeAssociations(AssetId id, ReadOnlySpan<AssetId> assets, bool clearPrevious = false, bool actAsReloadFlag = false)
        {
            if (assets.IsEmpty)
            {
                if (clearPrevious)
                    ClearAssociations(id);
                return;
            }

            _assocations.AddOrUpdate(id, NewFactory, UpdateFactory, assets);

            AssociationData NewFactory(AssetId _, ReadOnlySpan<AssetId> assets)
            {
                HashSet<AssetAssocationInfo> dependencies = new HashSet<AssetAssocationInfo>();
                foreach (AssetId id in assets)
                {
                    dependencies.Add(new AssetAssocationInfo(id, actAsReloadFlag));
                }

                AssociationData associations = new AssociationData(
                    new Lock(),
                    dependencies,
                    []);

                associations.Dependencies.Remove(new AssetAssocationInfo(id, actAsReloadFlag));
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] != id)
                    {
                        AddAssetAsDependent(assets[i], id);
                    }
                }

                return associations;
            }

            AssociationData UpdateFactory(AssetId _, AssociationData associationData, ReadOnlySpan<AssetId> assets)
            {
                using (associationData.Lock.EnterScope())
                {
                    if (clearPrevious)
                    {
                        while (true)
                        {
                            AssetAssocationInfo nextRemoval = default;
                            foreach (AssetAssocationInfo dependency in associationData.Dependencies)
                            {
                                if (!assets.Contains(dependency.Target))
                                {
                                    nextRemoval = dependency;
                                    RemoveAssetAsDependent(dependency.Target, id);

                                    break;
                                }
                            }

                            if (nextRemoval.Target.IsInvalid)
                                break;

                            associationData.Dependencies.Remove(nextRemoval);
                        }
                    }

                    for (int i = 0; i < assets.Length; i++)
                    {
                        if (assets[i] != id)
                        {
                            associationData.Dependencies.Remove(new AssetAssocationInfo(assets[i], !actAsReloadFlag));
                            associationData.Dependencies.Add(new AssetAssocationInfo(assets[i], actAsReloadFlag));

                            AddAssetAsDependent(assets[i], id);
                        }
                    }

                    return associationData;
                }
            }
        }

        public void MakeAssociations(AssetId id, IEnumerable<AssetId> assets, bool clearPrevious = false, bool actAsReloadFlag = false)
        {
            if (!assets.Any())
            {
                if (clearPrevious)
                    ClearAssociations(id);
                return;
            }

            _assocations.AddOrUpdate(id, NewFactory, UpdateFactory, assets);

            AssociationData NewFactory(AssetId _, IEnumerable<AssetId> assets)
            {
                HashSet<AssetAssocationInfo> dependencies = new HashSet<AssetAssocationInfo>();
                foreach (AssetId id in assets)
                {
                    dependencies.Add(new AssetAssocationInfo(id, actAsReloadFlag));
                }

                AssociationData associations = new AssociationData(
                    new Lock(),
                    dependencies,
                    []);

                associations.Dependencies.Remove(new AssetAssocationInfo(id, actAsReloadFlag));
                foreach (AssetId assetId in assets)
                {
                    if (assetId != id)
                    {
                        AddAssetAsDependent(assetId, id);
                    }
                }

                return associations;
            }

            AssociationData UpdateFactory(AssetId _, AssociationData associationData, IEnumerable<AssetId> assets)
            {
                using (associationData.Lock.EnterScope())
                {
                    if (clearPrevious)
                    {
                        while (true)
                        {
                            AssetAssocationInfo nextRemoval = default;
                            foreach (AssetAssocationInfo dependency in associationData.Dependencies)
                            {
                                if (!assets.Contains(dependency.Target))
                                {
                                    nextRemoval = dependency;
                                    RemoveAssetAsDependent(dependency.Target, id);

                                    break;
                                }
                            }

                            if (nextRemoval.Target.IsInvalid)
                                break;

                            associationData.Dependencies.Remove(nextRemoval);
                        }
                    }

                    foreach (AssetId assetId in assets)
                    {
                        if (assetId != id)
                        {
                            associationData.Dependencies.Remove(new AssetAssocationInfo(assetId, !actAsReloadFlag));
                            associationData.Dependencies.Add(new AssetAssocationInfo(assetId, actAsReloadFlag));

                            AddAssetAsDependent(assetId, id);
                        }
                    }

                    return associationData;
                }
            }
        }

        public void MakeAssociation(AssetId id, AssetId asset, bool clearPrevious = false, bool actAsReloadFlag = false) => MakeAssociations(id, new ReadOnlySpan<AssetId>(ref asset), clearPrevious, actAsReloadFlag);

        public void ClearAssociations(AssetId id)
        {
            if (_assocations.TryGetValue(id, out AssociationData associationData))
            {
                using (associationData.Lock.EnterScope())
                {
                    foreach (AssetAssocationInfo dependency in associationData.Dependencies)
                    {
                        RemoveAssetAsDependent(dependency.Target, id);
                    }

                    if (associationData.Dependencies.Count == 0 && associationData.Dependents.Count == 0)
                    {
                        _assocations.TryRemove(id, out _);
                    }
                }
            }
        }

        public bool DoesAssetHaveAssociations(AssetId id) => _assocations.ContainsKey(id);

        internal bool IsAssetOnlyActingAsReload(AssetId id, AssetId actingAsReload)
        {
            if (_assocations.TryGetValue(actingAsReload, out AssociationData data))
            {
                using(data.Lock.EnterScope())
                {
                    return data.Dependencies.Contains(new AssetAssocationInfo(id, true));
                }
            }

            return false;
        }

        internal Lock.Scope GetAssocationDataWithLockScope(AssetId id, out AssociationData associationData, out bool exists)
        {
            if (_assocations.TryGetValue(id, out associationData))
            {
                exists = true;
                return associationData.Lock.EnterScope();
            }

            exists = false;
            return default;
        }

        private static string s_registryFile => Path.Combine(ProjectData.Instance.Paths.LibrarySavedFolder, "AssociatedFiles.dat");
        private const int CurrentVersion = 1;
    }

    public readonly record struct AssociationData(Lock Lock, HashSet<AssetAssocationInfo> Dependencies, HashSet<AssetId> Dependents);
    public readonly record struct AssetAssocationInfo(AssetId Target, bool OnlyOnReload) : IEquatable<AssetAssocationInfo>
    {
        public override int GetHashCode() => HashCode.Combine(Target, OnlyOnReload);
    }
}
