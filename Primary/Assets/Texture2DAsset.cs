using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Assets
{
    public sealed class Texture2DAsset : TextureAsset
    {
        internal Texture2DAsset(TextureAssetData assetData) : base(assetData)
        {
        }
    }

    internal sealed class Texture2DAssetData : TextureAssetData
    {
        internal Texture2DAssetData(AssetId id) : base(id)
        {

        }
    }
}
