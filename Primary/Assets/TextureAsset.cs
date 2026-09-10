using Primary.Assets.Types;
using Primary.RHI;
using Primary.Serialization.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Primary.Assets
{
    [JsonConverter(typeof(AssetJsonConverter))]
    public class TextureAsset : BaseAssetDefinition<TextureAsset, TextureAssetData>
    {
        public TextureAsset(TextureAssetData assetData) : base(assetData)
        {
        }

        public RHITexture? RawRHITexture => IsLoaded ? AssetData.Texture : null;
        public RHISampler? RawRHISampler => IsLoaded ? AssetData.Sampler : null;

        public int Width => IsLoaded ? AssetData.Width : 0;
        public int Height => IsLoaded ? AssetData.Height : 0;
        public RHIFormat Format => IsLoaded ? AssetData.Format : RHIFormat.Unknown;
    }

    public class TextureAssetData : BaseInternalAssetData<TextureAsset>
    {
        private RHITexture? _texture;
        private RHISampler? _sampler;

        private int _width;
        private int _height;
        private RHIFormat _format;

        internal TextureAssetData(AssetId id) : base(id)
        {
            _texture = null;
            _sampler = null;

            _width = 0;
            _height = 0;
            _format = RHIFormat.Unknown;
        }

        public override void Dispose()
        {
            base.Dispose();

            _texture?.Dispose();
            _texture = null;

            _sampler?.Dispose();
            _sampler = null;

            _width = 0;
            _height = 0;
            _format = RHIFormat.Unknown;
        }

        public void UpdateAssetData(TextureAsset asset, RHITexture texture, RHISampler sampler)
        {
            base.UpdateAssetData(asset);

            _texture = texture;
            _sampler = sampler;

            _width = texture.Description.Width;
            _height = texture.Description.Height;
            _format = texture.Description.Format;
        }

        internal RHITexture? Texture => _texture;
        internal RHISampler? Sampler => _sampler;

        internal int Width => _width;
        internal int Height => _height;
        internal RHIFormat Format => _format;
    }
}
