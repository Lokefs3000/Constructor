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

namespace PrimaryEditor.Assets
{
    public sealed class AssetRegistry : IAssetIdProvider
    {
        private ConcurrentDictionary<string, AssetId> _pathToIdDict;
        private ConcurrentDictionary<AssetId, string> _idToPathDict;

        private ConcurrentDictionary<string, AssetId>.AlternateLookup<ReadOnlySpan<char>> _pathToIdDictAlt;

        internal AssetRegistry()
        {
            _pathToIdDict = new ConcurrentDictionary<string, AssetId>();
            _idToPathDict = new ConcurrentDictionary<AssetId, string>();

            _pathToIdDictAlt = _pathToIdDict.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        internal void LoadRegistryFromDisk()
        {
            if (File.Exists(s_registryFile))
            {
                AssetRegistryJson json;
                try
                {
                    json = JsonSerializer.Deserialize(File.ReadAllText(s_registryFile), AssetRegistryJsonContext.Default.AssetRegistryJson)!;
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Failed to read asset registry from disk!");
                    throw;
                }

                if (json.Version != AssetRegistryJson.FileVersion)
                {
                    EdLog.Assets.Error("Incorrect asset registry version '{v}'", json.Version);
                    throw new Exception();
                }

                foreach (AssetIdDataJson data in json.Assets)
                {
                    if (_pathToIdDict.ContainsKey(data.Path) || _idToPathDict.ContainsKey(data.Id))
                    {
                        EdLog.Assets.Warning("Duplicate asset id or path '{i}' '{p}'", data.Id, data.Path);
                        continue;
                    }

                    _pathToIdDict.TryAdd(data.Path, data.Id);
                    _idToPathDict.TryAdd(data.Id, data.Path);
                }
            }
        }

        internal void SaveRegistryToDisk()
        {
            AssetRegistryJson data = new AssetRegistryJson();

            using RentedList<AssetIdDataJson> assets = new RentedList<AssetIdDataJson>();
            foreach (var (path, id) in _pathToIdDict)
            {
                assets.Add(new AssetIdDataJson
                {
                    Path = path,
                    Id = id
                });
            }

            data.Assets = [.. assets];

            try
            {
                File.WriteAllText(s_registryFile, JsonSerializer.Serialize(data, AssetRegistryJsonContext.Default.AssetRegistryJson));
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Failed to read file remappings from disk!");
                throw;
            }
        }

        internal void MoveFileWithinRegistry(string localPath, string newLocalPath)
        {
            if (_pathToIdDict.TryGetValue(localPath, out AssetId id))
            {
                _pathToIdDict.TryRemove(localPath, out _);

                _pathToIdDict.TryAdd(newLocalPath, id);
                _idToPathDict[id] = newLocalPath;
            }
        }

        private static AssetId GenerateId()
        {
            return new AssetId(Guid.CreateVersion7());
        }

        public AssetId GetOrRegisterIdFor(ReadOnlySpan<char> path)
        {
            if (!_pathToIdDictAlt.TryGetValue(path, out AssetId id))
            {
                string pathStr = path.ToString();
                id = _pathToIdDict.GetOrAdd(pathStr, New, GenerateId());

                AssetId New(string path, AssetId id)
                {
                    if (!_idToPathDict.TryAdd(id, path))
                    {
                        EdLog.Assets.Error("Failed to add new id to 'Id->Path' dictionary");
                    }

                    return id;
                }
            }

            return id;
        }

        public bool HasIdForPath(ReadOnlySpan<char> path)
        {
            return _pathToIdDictAlt.ContainsKey(path);
        }

        public bool IsIdValid(AssetId id)
        {
            return _idToPathDict.ContainsKey(id);
        }

        public bool TryGetIdFromPath(ReadOnlySpan<char> filePath, [NotNullWhen(true)] out AssetId value)
        {
            return _pathToIdDictAlt.TryGetValue(filePath, out value);
        }

        #region Id Provider
        public AssetId RetriveIdForPath(ReadOnlySpan<char> path)
        {
            return _pathToIdDictAlt.TryGetValue(path, out AssetId value) ? value : AssetId.Invalid;
        }

        public string? RetrievePathForId(AssetId assetId)
        {
            return _idToPathDict.TryGetValue(assetId, out string? value) ? value : null;
        }
        #endregion

        private static string s_registryFile => Path.Combine(ProjectData.Instance.Paths.RootFolder, "Assets.json");
    }
}
