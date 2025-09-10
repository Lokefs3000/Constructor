using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Assets
{
    public sealed class AssetIdentifier
    {
        //Key = LocalPath
        private ConcurrentDictionary<string, AssetId> _assets;

        private HashSet<ulong> _idHashSet;

        internal AssetIdentifier()
        {
            _assets = new ConcurrentDictionary<string, AssetId>();

            _idHashSet = new HashSet<ulong>();

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
                ulong localId = ulong.Parse(tokenizer.Current.ToString());

                _assets.TryAdd(localFilePath, new AssetId(localId));
                _idHashSet.Add(localId);
            }
        }

        /// <summary>Thread-safe</summary>
        internal string TrySerializeAssetIds()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var kvp in _assets)
            {
                sb.Append(kvp.Key);
                sb.Append(':');
                sb.AppendLine(kvp.Value.Id.ToString());
            }

            sb.Length--;

            return sb.ToString();
        }

        /// <summary>Thread-safe</summary>
        internal AssetId GetOrRegisterAsset(string localPath)
        {
            AssetId id = _assets.GetOrAdd(localPath, (_) =>
            {
                lock (_idHashSet)
                {
                    return new AssetId(GenerateId());
                }
            });

            return id;
        }

        /// <summary>Thread-safe</summary>
        public bool TryGetAssetId(string localPath, out AssetId asset) => _assets.TryGetValue(localPath, out asset);

        /// <summary>Thread-safe</summary>
        public bool HasIdForAsset(string localPath) => _assets.ContainsKey(localPath);

        /// <summary>Not thread-safe</summary>
        private ulong GenerateId()
        {
            ulong id = (ulong)Stopwatch.GetTimestamp();
            while (_idHashSet.Contains(id))
            {
                id++;
            }

            _idHashSet.Add(id);
            return id;
        }

        public static string DataFilePath = Path.Combine(EditorFilepaths.LibraryPath, "assets.ids");
    }

    public readonly record struct AssetId(ulong Id);
}
