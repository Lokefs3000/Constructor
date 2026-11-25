using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Primary.Assets.Types;
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

        internal AssetIdentifier()
        {
            _assets = new ConcurrentDictionary<string, AssetId>();
            _assetPaths = new ConcurrentDictionary<AssetId, string>();

            if (File.Exists(DataFilePath))
            {
                TryLoadAssetIds(DataFilePath);
            }
        }

        /// <summary>Not thread-safe</summary>
        private void TryLoadAssetIds(string filePath)
        {
            string source = File.ReadAllText(filePath);

            bool isCorrectVersion = false;

            foreach (var line in source.Tokenize('\n'))
            {
                if (line.IsEmpty)
                    continue;

                ReadOnlySpan<char> trimmed = line.Trim();
                if (trimmed.StartsWith(":@"))
                {
                    int find = trimmed.IndexOf(' ');
                    if (find == -1)
                        throw new Exception("Malformed meta");

                    ReadOnlySpan<char> name = trimmed.Slice(2, find - 2);
                    if (name.SequenceEqual("version"))
                    {
                        if (!uint.TryParse(trimmed.Slice(find), out uint result))
                            throw new Exception("Malformed version meta");

                        //TODO: ask auto-upgrade in place
                        if (result != FileVersion)
                            throw new Exception("Outdated id file");

                        isCorrectVersion = true;
                    }
                }
                else
                {
                    ReadOnlySpanTokenizer<char> tokenizer = line.Tokenize('|');

                    tokenizer.MoveNext();
                    string localFilePath = tokenizer.Current.ToString();

                    tokenizer.MoveNext();
                    AssetId localId = (AssetId)Guid.Parse(tokenizer.Current.ToString());

                    _assets.TryAdd(localFilePath, localId);
                    _assetPaths.TryAdd(localId, localFilePath);
                }
            }

            if (!isCorrectVersion)
                throw new Exception("Missing data in id file");
        }

        /// <summary>Thread-safe</summary>
        internal string TrySerializeAssetIds()
        {
            if (_assets.IsEmpty)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            //header
            {
                sb.Append(":@version ");
                sb.Append(FileVersion);

                sb.AppendLine();
            }

            foreach (var kvp in _assets)
            {
                sb.Append(kvp.Key);
                sb.Append('|');
                sb.AppendLine(kvp.Value.ToString("N"));
            }

            sb.Length--;
            return sb.ToString();
        }

        /// <summary>Thread-safe</summary>
        internal AssetId GetOrRegisterAsset(string localPath)
        {
            if (!AssetPipeline.IsLocalPath(localPath))
            {
                if (!AssetPipeline.TryGetLocalPathFromFull(localPath, out localPath!))
                {
                    EdLog.Assets.Warning("Failed to get local path for id retrieval: {path}", localPath);
                    return AssetId.Invalid;
                }
            }

            AssetId id = _assets.GetOrAdd(localPath, (_) =>
            {
                AssetId id = new AssetId(Guid.CreateVersion7());
                _assetPaths.TryAdd(id, localPath);
                return id;
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

        /// <summary>Thread-safe, Not atomic</summary>
        public int IdCount => _assets.Count;

        public static string DataFilePath = Path.Combine(EditorFilepaths.LibraryPath, "assets.ids");

        private const uint FileVersion = 1;
    }
}
