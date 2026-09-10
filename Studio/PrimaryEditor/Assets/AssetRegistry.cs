using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using PrimaryEditor.Assets.Serialization;
using PrimaryEditor.Project;
using TerraFX.Interop.Windows;

namespace PrimaryEditor.Assets
{
    public sealed class AssetRegistry : IAssetIdProvider
    {
        private ConcurrentDictionary<FileId, AssetRegistryData> _registeredAssets;

        private ConcurrentDictionary<string, FileId> _assetIdLookupDict;
        private ConcurrentDictionary<string, FileId>.AlternateLookup<ReadOnlySpan<char>> _assetIdLookupDictAlt;

        internal AssetRegistry()
        {
            _registeredAssets = new ConcurrentDictionary<FileId, AssetRegistryData>();

            _assetIdLookupDict = new ConcurrentDictionary<string, FileId>();
            _assetIdLookupDictAlt = _assetIdLookupDict.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        internal void SetupFileWithinRegistryWithId(FileId id, string localPath, string? assetPath)
        {
            _registeredAssets[id] = new AssetRegistryData(localPath, assetPath);
            _assetIdLookupDict[localPath] = id;
        }

        internal FileId SetupFileWithinRegistry(string localPath, string? assetPath)
        {
            return _assetIdLookupDict.GetOrAdd(localPath, ValueFactory, assetPath);

            FileId ValueFactory(string localPath, string? assetPath)
            {
                FileId id = (FileId)Guid.CreateVersion7();

                _registeredAssets[id] = new AssetRegistryData(localPath, assetPath);
                return id;
            }
        }

        internal void RemoveAssetPath(FileId id)
        {
            if (_registeredAssets.TryGetValue(id, out AssetRegistryData registryData))
            {
                AssetRegistryData newRegistryData = new AssetRegistryData(registryData.LocalPath, null);
                _registeredAssets.TryUpdate(id, newRegistryData, registryData);
            }
        }

        internal void UpdateLocalPath(string localPath, string newLocalPath)
        {
            if (_assetIdLookupDict.TryGetValue(localPath, out FileId id) && _registeredAssets.TryGetValue(id, out AssetRegistryData registryData))
            {
                _assetIdLookupDict.TryRemove(localPath, out _);

                _assetIdLookupDict.TryAdd(newLocalPath, id);
                _registeredAssets[id] = new AssetRegistryData(newLocalPath, registryData.AssetPath);
            }
        }

        internal void UpdateLocalPath(FileId id, string newLocalPath)
        {
            if (_registeredAssets.TryGetValue(id, out AssetRegistryData registryData))
            {
                _assetIdLookupDict.TryRemove(registryData.LocalPath, out _);

                _assetIdLookupDict.TryAdd(newLocalPath, id);
                _registeredAssets[id] = new AssetRegistryData(newLocalPath, registryData.AssetPath);
            }
        }

        internal void RemoveAssetFromRegistry(FileId assetId)
        {
            if (_registeredAssets.TryRemove(assetId, out AssetRegistryData registryData))
            {
                _assetIdLookupDict.TryRemove(registryData.LocalPath, out _);
            }
        }

        public bool TryGetPathForId(FileId assetId, bool getLocalPath, [NotNullWhen(true)] out string? value)
        {
            if (_registeredAssets.TryGetValue(assetId, out AssetRegistryData registryData))
            {
                value = getLocalPath ? registryData.LocalPath : registryData.AssetPath;
                return value != null;
            }

            value = null;
            return false;
        }

        public bool TryGetAnyPathForId(FileId assetId, [NotNullWhen(true)] out string? value)
        {
            if (_registeredAssets.TryGetValue(assetId, out AssetRegistryData registryData))
            {
                value = registryData.AssetPath ?? registryData.LocalPath;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetLocalAndAssetPathsForId(FileId assetId, [NotNullWhen(true)] out string? localPath, [MaybeNullWhen(true)] out string? assetPath)
        {
            if (_registeredAssets.TryGetValue(assetId, out AssetRegistryData registryData))
            {
                localPath = registryData.LocalPath;
                assetPath = registryData.AssetPath;
                return true;
            }

            localPath = null;
            assetPath = null;
            return false;
        }

        public bool TryLookupIdForPath(ReadOnlySpan<char> path, [NotNullWhen(true)] out FileId value) => _assetIdLookupDictAlt.TryGetValue(path, out value);

        public bool IsIdValid(FileId assetId) => _registeredAssets.ContainsKey(assetId);
        public bool DoesPathHaveLookup(ReadOnlySpan<char> path) => _assetIdLookupDictAlt.ContainsKey(path);

        public bool TryGetLocalPathForId(FileId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, true, out value);
        public bool TryGetAssetPathForId(FileId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, false, out value);

        private readonly record struct AssetRegistryData(string LocalPath, string? AssetPath);
    }
}
