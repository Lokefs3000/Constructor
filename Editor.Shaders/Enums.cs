namespace Editor.Shaders
{
    public enum PropertyDefault : byte
    {
        NumOne = 0,
        NumZero,
        NumIdentity,

        TexWhite,
        TexBlack,
        TexMask,
        TexNormal
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
        StructuredBuffer,
        ByteAddressBuffer,
        SamplerState
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
        //Bitangnet,
        BlendIndices = Tangent + 2,
        BlendWeight,
        PositionT,
        PSize,
        Fog,
        TessFactor,
        SV_InstanceId,
        SV_VertexId,
        SV_Position
    }
}
