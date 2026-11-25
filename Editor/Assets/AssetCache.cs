using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.Assets
{
    public sealed class AssetCache : IAssetCacheProvider
    {
        private HashSet<AssetId> _assetsWithCaches;
        private Lock _hashSetLock;

        internal AssetCache()
        {
            _assetsWithCaches = new HashSet<AssetId>();
            _hashSetLock = new Lock();

            DiscoverAssetCaches();
        }

        private void DiscoverAssetCaches()
        {
            foreach (string directory in Directory.GetDirectories(EditorFilepaths.LibraryCachePath))
            {
                string dirName = Path.GetFileName(directory) ?? string.Empty;
                if (Guid.TryParse(dirName, out Guid result))
                {
                    _assetsWithCaches.Add((AssetId)result);
                }
                else
                {
                    EdLog.Assets.Warning("[{guid}]: Failed to parse guid for asset cache directory: {dir}", dirName, directory);
                }
            }
        }

        public string? GetCacheLocation(AssetId assetId, [NotNullWhen(true)] bool createIfNew = true)
        {
            string path = Path.Combine(EditorFilepaths.LibraryCachePath, assetId.ToString());

            lock (_hashSetLock)
            {
                if (createIfNew)
                {
                    if (_assetsWithCaches.Add(assetId))
                        Directory.CreateDirectory(path);
                }
                else
                {
                    if (!_assetsWithCaches.Contains(assetId))
                        return null;
                }
            }

            return path;
        }
    }
}
