using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Primary.Assets.Types
{
    public interface IAssetCacheProvider
    {
        /// <summary>Thread-safe</summary>
        public string? GetCacheLocation(AssetId assetId, [NotNullWhen(true)] bool createIfNew = true);
    }
}
