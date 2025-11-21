using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    public abstract class AttributeSignature
    {
        private readonly string _name;
        private readonly AttributeUsage _usage;
        private readonly AttributeRelation[] _incompatible;
        private readonly AttributeVariable[] _signature;

        protected AttributeSignature(string propertyName, AttributeUsage usage, AttributeRelation[] incompatible, AttributeVariable[] signature)
        {
            _name = propertyName;
            _usage = usage;
            _incompatible = incompatible;
            _signature = signature;
        }

        public string Name => _name;
        public AttributeUsage Usage => _usage;
        public AttributeRelation[] Incompatible => _incompatible;
        public AttributeVariable[] Signature => _signature;
    }

    public readonly record struct AttributeVariable(string Name, Type Type, object? Default, AttributeFlags Flags);
    public readonly record struct AttributeRelation(Type Type, AttributeRelationFlags Flags);

    public enum AttributeFlags : byte
    {
        None = 0,

        Required = 1 << 0
    }

    public enum AttributeUsage : uint
    {
        Function = 1 << 0,
        ConstantBuffer = 1 << 1,
        StructuredBuffer = 1 << 2,
        ByteAddressBuffer = 1 << 9,
        Texture1D = 1 << 3,
        Texture2D = 1 << 4,
        Texture3D = 1 << 5,
        TextureCube = 1 << 6,
        SamplerState = 1 << 7,
        StaticSampler = 1 << 10,

        Property = 1 << 8,

        GenericTexture = Texture1D | Texture2D | Texture3D | TextureCube,
        GenericResource = ConstantBuffer | StructuredBuffer | ByteAddressBuffer | Texture1D | Texture2D | Texture3D | TextureCube,
        GenericSampler = SamplerState | StaticSampler,
    }

    public enum AttributeRelationFlags : byte
    {
        Incompatible = 0,
        Required
    }
}
