using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Primary.Assets.Types;

namespace Primary.Assets.Default
{
    public sealed class ProjectIdProvider : IAssetIdProvider
    {
        private readonly FrozenDictionary<string, AssetId> _pathToId;
        private readonly FrozenDictionary<AssetId, string> _idToPath;

        private readonly FrozenDictionary<string, AssetId>.AlternateLookup<ReadOnlySpan<char>> _pathToIdSpan;

        public ProjectIdProvider(string projectDirectory)
        {
            JsonNode assetsRootNode = JsonSerializer.Deserialize<JsonNode>(File.ReadAllText(Path.Combine(projectDirectory, "Assets.json")))!;

            Dictionary<string, AssetId> pathToId = new Dictionary<string, AssetId>();
            Dictionary<AssetId, string> idToPath = new Dictionary<AssetId, string>();
            foreach (JsonObject idData in ((JsonArray)assetsRootNode["assets"]!)!)
            {
                AssetId id = new AssetId(Guid.Parse(idData["id"]!.GetValue<string>()));
                string path = idData["path"]!.GetValue<string>();

                pathToId.Add(path, id);
                idToPath.Add(id, path);
            }

            _pathToId = pathToId.ToFrozenDictionary();
            _idToPath = idToPath.ToFrozenDictionary();

            _pathToIdSpan = _pathToId.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public string? RetrievePathForId(AssetId assetId)
        {
            _idToPath.TryGetValue(assetId, out string? path);
            return path;
        }

        public AssetId RetriveIdForPath(ReadOnlySpan<char> path)
        {
            _pathToIdSpan.TryGetValue(path, out AssetId id);
            return id;
        }
    }
}
