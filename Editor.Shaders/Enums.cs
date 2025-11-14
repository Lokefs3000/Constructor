using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders
{
    public enum PropertyDefault : byte
    {
        White = 0,
        Black,
        Mask,
        Normal
    }

    public enum PropertyDisplay : byte
    {
        Default = 0,
        Color,
    }

    public enum ResourceType : byte
    {
        Texture1D,
        Texture2D,
        Texture3D,
        TextureCube,
        ConstantBuffer,
        StructedBuffer
    }

    public enum ValueGeneric : byte
    {
        Float,
        Double,
        Int,
        UInt,

        Custom
    }

    public enum SemanticName : byte
    {
        Position = 0,
        Texcoord,
        Color,
        Normal,
        Tangent,
        Bitangnet,
        BlendIndices,
        BlendWeight,
        PositionT,
        PSize,
        Fog,
        TessFactor,
    }
}
