using Primary.Assets.Types;

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
