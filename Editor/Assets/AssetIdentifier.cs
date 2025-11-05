using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Primary.Assets;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Editor.Assets
{
    public sealed class AssetIdentifier : IAssetIdProvider
    {
        //Key = LocalPath
        private ConcurrentDictionary<string, AssetId> _assets;
        private ConcurrentDictionary<AssetId, string> _assetPaths;

        private HashSet<uint> _idHashSet;

        internal AssetIdentifier()
        {
            _assets = new ConcurrentDictionary<string, AssetId>();
            _assetPaths = new ConcurrentDictionary<AssetId, string>();

            _idHashSet = new HashSet<uint>();

            if (File.Exists(DataFilePath))
            {
                TryLoadAssetIds(DataFilePath);
            }
        }

        /// <summary>Not thread-safe</summary>
        private void TryLoadAssetIds(string filePath)
        {
            string[] source = File.ReadAllLines(filePath);

            foreach (string line in source)
            {
                ReadOnlySpanTokenizer<char> tokenizer = line.Tokenize(':');

                tokenizer.MoveNext();
                string localFilePath = tokenizer.Current.ToString();

                tokenizer.MoveNext();
                uint localId = uint.Parse(tokenizer.Current.ToString());

                _assets.TryAdd(localFilePath, new AssetId(localId));
                _assetPaths.TryAdd(new AssetId(localId), localFilePath);
                _idHashSet.Add(localId);
            }
        }

        /// <summary>Thread-safe</summary>
        internal string TrySerializeAssetIds()
        {
            if (_assets.IsEmpty)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            foreach (var kvp in _assets)
            {
                sb.Append(kvp.Key);
                sb.Append(':');
                sb.AppendLine(kvp.Value.ToString());
            }

            sb.Length--;

            return sb.ToString();
        }

        /// <summary>Thread-safe</summary>
        internal AssetId GetOrRegisterAsset(string localPath)
        {
            if (!AssetPipeline.IsLocalPath(localPath))
            {
                if (!AssetPipeline.TryGetLocalPathFromFull(localPath, out localPath))
                {
                    EdLog.Assets.Warning("Cannot get asset id for not local path: {path}", localPath);
                    return AssetId.Invalid;
                }
            }

            AssetId id = _assets.GetOrAdd(localPath, (_) =>
            {
                lock (_idHashSet)
                {
                    AssetId id = new AssetId(GenerateId());
                    _assetPaths.TryAdd(id, localPath);
                    return id;
                }
            });

            return id;
        }

        /// <summary>Thread-safe</summary>
        internal void ChangeIdPath(AssetId id, string localPath, string newLocalPath)
        {
            if (_assetPaths.ContainsKey(id))
            {
                _assetPaths.TryUpdate(id, newLocalPath, localPath);
            }
        }

        /// <summary>Thread-safe</summary>
        public bool TryGetAssetId(string localPath, out AssetId asset) => _assets.TryGetValue(localPath, out asset);

        /// <summary>Thread-safe</summary>
        public bool HasIdForAsset(string localPath) => _assets.ContainsKey(localPath);

        /// <summary>Thread-safe</summary>
        public bool IsIdValid(AssetId id) => !id.IsInvalid && _assetPaths.ContainsKey(id);

        /// <summary>Not thread-safe</summary>
        private uint GenerateId()
        {
            uint id = (uint)Stopwatch.GetTimestamp();
            while (id == IAssetIdProvider.Invalid || _idHashSet.Contains(id))
            {
                id++;
            }

            _idHashSet.Add(id);
            return id;
        }

        #region Provider
        public string? RetrievePathForId(AssetId assetId)
        {
            if (_assetPaths.TryGetValue(assetId, out string? path))
                return path;
            return null;
        }

        public AssetId RetriveIdForPath(ReadOnlySpan<char> path)
        {
            if (_assets.TryGetValue(path.ToString(), out AssetId assetId))
                return assetId;
            return AssetId.Invalid;
        }
        #endregion

        public static string DataFilePath = Path.Combine(EditorFilepaths.LibraryPath, "assets.ids");
    }
}
