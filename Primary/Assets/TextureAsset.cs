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

        public RHITexture? RawRHITexture => IsReady ? AssetData.Texture : null;
        public RHISampler? RawRHISampler => IsReady ? AssetData.Sampler : null;

        public int Width => IsReady ? AssetData.Width : 0;
        public int Height => IsReady ? AssetData.Height : 0;
        public RHIFormat Format => IsReady ? AssetData.Format : RHIFormat.Unknown;

        public CubemapSHL2? SHL2 => IsReady ? AssetData.SHL2 : null;
    }

    public class TextureAssetData : BaseInternalAssetData<TextureAsset>
    {
        private RHITexture? _texture;
        private RHISampler? _sampler;

        private int _width;
        private int _height;
        private RHIFormat _format;

        private CubemapSHL2? _shl2;

        internal TextureAssetData(AssetId id, int uniqueIndex) : base(id, uniqueIndex)
        {
            _texture = null;
            _sampler = null;

            _width = 0;
            _height = 0;
            _format = RHIFormat.Unknown;

            _shl2 = null;
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

            _shl2 = null;
        }

        public void UpdateAssetData(TextureAsset asset, RHITexture texture, RHISampler sampler, CubemapSHL2? shl2)
        {
            base.UpdateAssetData(asset);

            _texture = texture;
            _sampler = sampler;

            _width = texture.Description.Width;
            _height = texture.Description.Height;
            _format = texture.Description.Format;

            _shl2 = shl2;
        }

        internal RHITexture? Texture => _texture;
        internal RHISampler? Sampler => _sampler;

        internal int Width => _width;
        internal int Height => _height;
        internal RHIFormat Format => _format;

        internal CubemapSHL2? SHL2 => _shl2;
    }

    [InlineArray(9)]
    public struct CubemapSHL2
    {
        private Color _value;

        public record struct Color
        {
            public float R;
            public float G;
            public float B;
        }
    }
}
