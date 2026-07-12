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
        private ConcurrentDictionary<AssetId, AssetRegistryData> _registeredAssets;

        private ConcurrentDictionary<string, AssetId> _assetIdLookupDict;
        private ConcurrentDictionary<string, AssetId>.AlternateLookup<ReadOnlySpan<char>> _assetIdLookupDictAlt;

        internal AssetRegistry()
        {
            _registeredAssets = new ConcurrentDictionary<AssetId, AssetRegistryData>();

            _assetIdLookupDict = new ConcurrentDictionary<string, AssetId>();
            _assetIdLookupDictAlt = _assetIdLookupDict.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        internal void SetupFileWithinRegistryWithId(AssetId id, string localPath, string? assetPath)
        {
            _registeredAssets[id] = new AssetRegistryData(localPath, assetPath);
            _assetIdLookupDict[localPath] = id;
        }

        internal AssetId SetupFileWithinRegistry(string localPath, string? assetPath)
        {
            return _assetIdLookupDict.GetOrAdd(localPath, ValueFactory, assetPath);

            AssetId ValueFactory(string localPath, string? assetPath)
            {
                AssetId id = (AssetId)Guid.CreateVersion7();

                _registeredAssets[id] = new AssetRegistryData(localPath, assetPath);
                return id;
            }
        }

        internal void RemoveAssetPath(AssetId id)
        {
            if (_registeredAssets.TryGetValue(id, out AssetRegistryData registryData))
            {
                AssetRegistryData newRegistryData = new AssetRegistryData(registryData.LocalPath, null);
                _registeredAssets.TryUpdate(id, newRegistryData, registryData);
            }
        }

        internal void UpdateLocalPath(string localPath, string newLocalPath)
        {
            if (_assetIdLookupDict.TryGetValue(localPath, out AssetId id) && _registeredAssets.TryGetValue(id, out AssetRegistryData registryData))
            {
                _assetIdLookupDict.TryRemove(localPath, out _);

                _assetIdLookupDict.TryAdd(newLocalPath, id);
                _registeredAssets[id] = new AssetRegistryData(newLocalPath, registryData.AssetPath);
            }
        }

        public bool TryGetPathForId(AssetId assetId, bool getLocalPath, [NotNullWhen(true)] out string? value)
        {
            if (_registeredAssets.TryGetValue(assetId, out AssetRegistryData registryData))
            {
                value = getLocalPath ? registryData.LocalPath : registryData.AssetPath;
                return value != null;
            }

            value = null;
            return false;
        }

        public bool TryGetAnyPathForId(AssetId assetId, [NotNullWhen(true)] out string? value)
        {
            if (_registeredAssets.TryGetValue(assetId, out AssetRegistryData registryData))
            {
                value = registryData.AssetPath ?? registryData.LocalPath;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetLocalAndAssetPathsForId(AssetId assetId, [NotNullWhen(true)] out string? localPath, [MaybeNullWhen(true)] out string? assetPath)
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

        public bool TryLookupIdForPath(ReadOnlySpan<char> path, [NotNullWhen(true)] out AssetId value) => _assetIdLookupDictAlt.TryGetValue(path, out value);

        public bool IsIdValid(AssetId assetId) => _registeredAssets.ContainsKey(assetId);
        public bool DoesPathHaveLookup(ReadOnlySpan<char> path) => _assetIdLookupDictAlt.ContainsKey(path);

        public bool TryGetLocalPathForId(AssetId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, true, out value);
        public bool TryGetAssetPathForId(AssetId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, false, out value);

        private readonly record struct AssetRegistryData(string LocalPath, string? AssetPath);
    }
}
