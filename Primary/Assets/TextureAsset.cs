using Primary.Assets.Types;
using Primary.RHI2;
using System.Runtime.CompilerServices;

namespace Primary.Assets
{
    public class TextureAsset : IAssetDefinition
    {
        private readonly TextureAssetData _assetData;

        internal TextureAsset(TextureAssetData assetData)
        {
            _assetData = assetData;
        }

        internal TextureAssetData AssetData => _assetData;

        public RHITexture? RawRHITexture => _assetData.Texture;
        public RHISampler? RawRHISampler => _assetData.Sampler;

        public ResourceStatus Status => _assetData.Status;

        public string Name => _assetData.Name;
        public AssetId Id => _assetData.Id;

        public int Width => _assetData.Width;
        public int Height => _assetData.Height;
        public RHIFormat Format => _assetData.Format;
    }

    internal class TextureAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private readonly AssetId _id;
        private string _name;

        private RHITexture? _texture;
        private RHISampler? _sampler;

        private int _width;
        private int _height;
        private RHIFormat _format;

        internal TextureAssetData(AssetId id)
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _id = id;
            _name = string.Empty;

            _texture = null;
            _sampler = null;

            _width = 0;
            _height = 0;
            _format = RHIFormat.Unknown;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;

            _texture?.Dispose();
            _texture = null;

            _sampler?.Dispose();
            _sampler = null;

            _width = 0;
            _height = 0;
            _format = RHIFormat.Unknown;
        }

        public void SetAssetInternalStatus(ResourceStatus status)
        {
            _status = status;
        }

        public void SetAssetInternalName(string name)
        {
            _name = name;
        }

        internal void UpdateAssetData(TextureAsset asset, RHITexture texture, RHISampler sampler)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;

            _texture = texture;
            _sampler = sampler;

            _width = texture.Description.Width;
            _height = texture.Description.Height;
            _format = texture.Description.Format;
        }

        internal void UpdateAssetFailed(TextureAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        public int LoadIndex => 0;

        internal RHITexture? Texture => _texture;
        internal RHISampler? Sampler => _sampler;

        internal int Width => _width;
        internal int Height => _height;
        internal RHIFormat Format => _format;

        public Type AssetType => typeof(TextureAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);

        AssetId IInternalAssetData.Id => Id;
        ResourceStatus IInternalAssetData.Status => Status;
        string IInternalAssetData.Name => Name;
    }
}
