using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets
{
    public sealed class AssetRegistry : IAssetIdProvider
    {
        private Dictionary<string, AssetId> _pathToIdDict;
        private Dictionary<AssetId, string> _idToPathDict;

        internal AssetRegistry()
        {
            _pathToIdDict = new Dictionary<string, AssetId>();
            _idToPathDict = new Dictionary<AssetId, string>();
        }

        private AssetId GenerateId()
        {
            return new AssetId(Guid.CreateVersion7());
        }

        #region Id Provider
        public AssetId RetriveIdForPath(ReadOnlySpan<char> path)
        {
            return _pathToIdDict.TryGetValue(path.ToString(), out AssetId value) ? value : AssetId.Invalid;
        }

        public string? RetrievePathForId(AssetId assetId)
        {
            return _idToPathDict.TryGetValue(assetId, out string? value) ? value : null;
        }
        #endregion
    }
}
