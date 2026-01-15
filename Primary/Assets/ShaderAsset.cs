using Primary.Assets.Types;
using Primary.Rendering.Assets;
using Primary.RHI2;
using System.Collections.Frozen;

namespace Primary.Assets
{
    public sealed class ShaderAsset : BaseAssetDefinition<ShaderAsset, ShaderAssetData>, IShaderResourceSource
    {
        internal ShaderAsset(ShaderAssetData assetData) : base(assetData)
        {
        }

        public PropertyBlock? CreatePropertyBlock()
        {
            if (Status != ResourceStatus.Success)
                return null;

            return new PropertyBlock(this);
        }

        public ReadOnlySpan<ShaderProperty> Properties => AssetData.Properties;
        public IReadOnlyDictionary<int, int> RemappingTable => AssetData.RemappingTable;

        public int PropertyBlockSize => AssetData.PropertyBlockSize;
        public int HeaderBlockSize => AssetData.HeaderBlockSize;

        public ShHeaderFlags HeaderFlags => AssetData.HeaderFlags;

        public RHIGraphicsPipeline? GraphicsPipeline => AssetData.GraphicsPipeline;

        public int ResourceCount => AssetData.ResourceCount;
    }

    public sealed class ShaderAssetData : BaseInternalAssetData<ShaderAsset>
    {
        private ShaderProperty[] _properties;
        private FrozenDictionary<int, int> _remappingTable;

        private int _propertyBlockSize;
        private int _headerBlockSize;

        private ShHeaderFlags _headerFlags;

        private RHIGraphicsPipeline? _graphicsPipeline;

        private int _resourceCount;

        internal ShaderAssetData(AssetId id) : base(id)
        {
            _properties = Array.Empty<ShaderProperty>();
            _remappingTable = FrozenDictionary<int, int>.Empty;

            _propertyBlockSize = 0;
            _headerBlockSize = 0;

            _graphicsPipeline = null;
        }

        public void UpdateAssetData(ShaderAsset asset, ShaderProperty[] properties, FrozenDictionary<int, int> remappingTable, int propertyBlockSize, int headerBlockSize, ShHeaderFlags headerFlags, RHIGraphicsPipeline graphicsPipeline)
        {
            UpdateAssetData(asset);

            _properties = properties;
            _remappingTable = remappingTable;

            _propertyBlockSize = propertyBlockSize;
            _headerBlockSize = headerBlockSize;

            _headerFlags = headerFlags;

            _graphicsPipeline = graphicsPipeline;

            _resourceCount = 0;
            foreach (ref readonly ShaderProperty property in properties.AsSpan())
            {
                if (property.Type == ShPropertyType.Buffer || property.Type == ShPropertyType.Texture || property.Type == ShPropertyType.Sampler)
                    _resourceCount++;
            }
        }

        internal ReadOnlySpan<ShaderProperty> Properties => _properties;
        internal FrozenDictionary<int, int> RemappingTable => _remappingTable;

        internal int PropertyBlockSize => _propertyBlockSize;
        internal int HeaderBlockSize => _headerBlockSize;

        internal ShHeaderFlags HeaderFlags => _headerFlags;

        internal RHIGraphicsPipeline? GraphicsPipeline => _graphicsPipeline;

        internal int ResourceCount => _resourceCount;
    }

    public readonly record struct ShaderProperty(string Name, ushort IndexOrByteOffset, ushort ByteWidth, ushort ChildIndex, ShPropertyType Type, ShPropertyDefault Default, ShPropertyStages Stages, ShPropertyFlags Flags, ShPropertyDisplay Display);
    public readonly record struct ShaderResource(string Name, ShResourceType Type, ShPropertyStages Stages, ShResourceFlags Flags);

    public enum ShPropertyType : byte
    {
        Buffer = 0,
        Texture,

        Sampler,

        Single,
        Double,

        UInt32,
        Int32,

        Vector2,
        Vector3,
        Vector4,

        Matrix4x4,

        Struct
    }

    public enum ShPropertyStages : byte
    {
        None = 0,

        VertexShading,
        PixelShading,
        ComputeShading,
        AllShading
    }

    public enum ShResourceType : byte
    {
        Unknown = 0,

        Texture1D,
        Texture2D,
        Texture3D,
        TextureCube,
        ConstantBuffer,
        StructuredBuffer,
        ByteAddressBuffer,
        SamplerState
    }

    public enum ShResourceFlags : byte
    {
        None = 0,

        Property = 1 << 0,
        ReadWrite = 1 << 1
    }

    public enum ShPropertyDisplay : byte
    {
        Default = 0,
        Color
    }

    public enum ShPropertyFlags : byte
    {
        None = 0,

        Constants = 1 << 0,
        Global = 1 << 1,
        Sampled = 1 << 2,
        HasParent = 1 << 3,
        Property = 1 << 4,
        ReadWrite = 1 << 5
    }

    public enum ShPropertyDefault : byte
    {
        NumOne = 0,
        NumZero,
        NumIdentity,

        TexWhite,
        TexBlack,
        TexMask,
        TexNormal
    }

    public enum ShHeaderFlags : byte
    {
        None = 0,

        ExternalProperties = 1 << 0,
        HeaderIsBuffer = 1 << 1,
    }
}
