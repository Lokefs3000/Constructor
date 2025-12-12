using Primary.Assets.Types;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.Assets
{
    public sealed class ShaderAsset2 : BaseAssetDefinition<ShaderAsset2Data>
    {
        internal ShaderAsset2(ShaderAsset2Data assetData) : base(assetData)
        {
        }

        public PropertyBlock? CreatePropertyBlock()
        {
            if (Status != ResourceStatus.Success)
                return null;

            return new PropertyBlock(this);
        }

        public ReadOnlySpan<ShaderProperty> Properties => AssetData.Properties;
        public FrozenDictionary<int, int> RemappingTable => AssetData.RemappingTable;

        public int PropertyBlockSize => AssetData.PropertyBlockSize;
        public int HeaderBlockSize => AssetData.HeaderBlockSize;

        public RHI.GraphicsPipeline? GraphicsPipeline => AssetData.GraphicsPipeline;
    }

    public sealed class ShaderAsset2Data : BaseInternalAssetData<ShaderAsset2>
    {
        private ShaderProperty[] _properties;
        private FrozenDictionary<int, int> _remappingTable;

        private int _propertyBlockSize;
        private int _headerBlockSize;

        private RHI.GraphicsPipeline? _graphicsPipeline;

        internal ShaderAsset2Data(AssetId id) : base(id)
        {
            _properties = Array.Empty<ShaderProperty>();
            _remappingTable = FrozenDictionary<int, int>.Empty;

            _propertyBlockSize = 0;
            _headerBlockSize = 0;

            _graphicsPipeline = null;
        }

        public void UpdateAssetData(ShaderAsset2 asset, ShaderProperty[] properties, FrozenDictionary<int, int> remappingTable, int propertyBlockSize, int headerBlockSize, RHI.GraphicsPipeline graphicsPipeline)
        {
            UpdateAssetData(asset);

            _properties = properties;
            _remappingTable = remappingTable;

            _propertyBlockSize = propertyBlockSize;
            _headerBlockSize = headerBlockSize;

            _graphicsPipeline = graphicsPipeline;
        }

        internal ReadOnlySpan<ShaderProperty> Properties => _properties;
        internal FrozenDictionary<int, int> RemappingTable => _remappingTable;

        internal int PropertyBlockSize => _propertyBlockSize;
        internal int HeaderBlockSize => _headerBlockSize;

        internal RHI.GraphicsPipeline? GraphicsPipeline => _graphicsPipeline;
    }

    public readonly record struct ShaderProperty(string Name, ushort IndexOrByteOffset, ushort ByteWidth, ShPropertyType Type, ShPropertyDefault Default, ShPropertyStages Stages, ShPropertyFlags Flags, ShPropertyDisplay Display);
    public readonly record struct ShaderResource(string Name, ShResourceType Type, ShResourceFlags Flags);

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

        GenericShader = 1 << 0,
        PixelShader = 1 << 1
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
}
