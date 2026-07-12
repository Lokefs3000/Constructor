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

                using RentedList<AssetId> dependencies = new RentedList<AssetId>();
                while (!serializer.IsAtEndOfStream)
                {
                    AssetId assetId = (AssetId)serializer.ReadGuid()!.Value;

                    dependencies.Clear();
                    while (serializer.LeadingByte != '\n')
                    {
                        dependencies.Add((AssetId)serializer.ReadGuid()!.Value);
                    }

                    serializer.ReadNewLine();

                    MakeAssociations(assetId, dependencies.AsSpan());
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
                        serializer.WriteValue(id);

                        foreach (AssetId dependency in data.Dependencies)
                        {
                            serializer.WriteValue(dependency);
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

        public void MakeAssociations(AssetId id, ReadOnlySpan<AssetId> assets, bool clearPrevious = false)
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
                AssociationData associations = new AssociationData(
                    new Lock(),
                    [.. assets],
                    []);

                associations.Dependencies.Remove(id);
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
                            AssetId nextRemoval = AssetId.Invalid;
                            foreach (AssetId dependency in associationData.Dependencies)
                            {
                                if (!assets.Contains(dependency))
                                {
                                    nextRemoval = dependency;
                                    RemoveAssetAsDependent(dependency, id);

                                    break;
                                }
                            }

                            if (nextRemoval.IsInvalid)
                                break;

                            associationData.Dependencies.Remove(nextRemoval);
                        }
                    }

                    for (int i = 0; i < assets.Length; i++)
                    {
                        if (assets[i] != id && associationData.Dependencies.Add(assets[i]))
                        {
                            AddAssetAsDependent(assets[i], id);
                        }
                    }

                    return associationData;
                }
            }
        }

        public void MakeAssociation(AssetId id, AssetId asset, bool clearPrevious = false) => MakeAssociations(id, new ReadOnlySpan<AssetId>(ref asset), clearPrevious);

        public void ClearAssociations(AssetId id)
        {
            if (_assocations.TryGetValue(id, out AssociationData associationData))
            {
                using (associationData.Lock.EnterScope())
                {
                    foreach (AssetId dependency in associationData.Dependencies)
                    {
                        RemoveAssetAsDependent(dependency, id);
                    }

                    if (associationData.Dependencies.Count == 0 && associationData.Dependents.Count == 0)
                    {
                        _assocations.TryRemove(id, out _);
                    }
                }
            }
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

    public readonly record struct AssociationData(Lock Lock, HashSet<AssetId> Dependencies, HashSet<AssetId> Dependents);
}
