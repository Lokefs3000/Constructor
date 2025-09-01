namespace Primary.RHI
{
    public abstract class GraphicsPipeline : IDisposable
    {
        public abstract ref readonly GraphicsPipelineDescription Description { get; }
        public abstract ref readonly GraphicsPipelineBytecode Bytecode { get; }
        public abstract string Name { set; }

        public abstract void Dispose();
    }

    public record struct GraphicsPipelineDescription
    {
        public FillMode FillMode;
        public CullMode CullMode;
        public bool FrontCounterClockwise;
        public int DepthBias;
        public float DepthBiasClamp;
        public float SlopeScaledDepthBias;
        public bool DepthClipEnable;
        public bool ConservativeRaster;
        public bool DepthEnable;
        public DepthWriteMask DepthWriteMask;
        public ComparisonFunc DepthFunc;
        public bool StencilEnable;
        public byte StencilReadMask;
        public byte StencilWriteMask;
        public PrimitiveTopologyType PrimitiveTopology;
        public StencilFace FrontFace;
        public StencilFace BackFace;
        public bool AlphaToCoverageEnable;
        public bool IndependentBlendEnable;
        public bool LogicOpEnable;
        public LogicOp LogicOp;
        public BlendDescription[] Blends;
        public InputElementDescription[] InputElements;
        public BoundResourceDescription[] BoundResources;
        public KeyValuePair<uint, ImmutableSamplerDescription>[] ImmutableSamplers;
        public uint ExpectedConstantsSize;
    }

    public record struct GraphicsPipelineBytecode
    {
        public Memory<byte> Vertex;
        public Memory<byte> Pixel;
    }

    public record struct StencilFace
    {
        public StencilOp StencilFailOp;
        public StencilOp StencilDepthFailOp;
        public StencilOp StencilPassOp;
        public ComparisonFunc StencilFunc;
    }

    public record struct BlendDescription
    {
        public bool BlendEnable;
        public Blend SrcBlend;
        public Blend DstBlend;
        public BlendOp BlendOp;
        public Blend SrcBlendAlpha;
        public Blend DstBlendAlpha;
        public BlendOp BlendOpAlpha;
        public byte RenderTargetWriteMask;
    }

    public record struct InputElementDescription
    {
        public InputElementSemantic Semantic;
        public InputElementFormat Format;

        public int InputSlot;
        public int ByteOffset;

        public InputClassification InputSlotClass;
        public int InstanceDataStepRate;
    }

    public record struct BoundResourceDescription
    {
        public ResourceType Type;
        public int Index;
    }

    public record struct ImmutableSamplerDescription
    {
        public TextureFilter Filter;
        public TextureAddressMode AddressModeU;
        public TextureAddressMode AddressModeV;
        public TextureAddressMode AddressModeW;
        public uint MaxAnistropy;
        public float MipLODBias;
        public float MinLOD;
        public float MaxLOD;
    }

    public enum FillMode : byte
    {
        Solid = 0,
        Wireframe
    }

    public enum CullMode : byte
    {
        None = 0,
        Back,
        Front
    }

    public enum DepthWriteMask : byte
    {
        None = 0,
        All
    }

    public enum PrimitiveTopologyType : byte
    {
        Triangle = 0,
        Line,
        Point,
        Patch
    }

    public enum StencilOp : byte
    {
        Keep = 0,
        Zero,
        Replace,
        IncrementSaturation,
        DecrementSaturation,
        Invert,
        Increment,
        Decrement
    }

    public enum ComparisonFunc : byte
    {
        None = 0,
        Never,
        Less,
        Equal,
        LessEqual,
        Greater,
        NotEqual,
        GreaterEqual,
        Always
    }

    public enum Blend : byte
    {
        Zero = 0,
        One,
        SourceColor,
        InverseSourceColor,
        SourceAlpha,
        InverseSourceAlpha,
        DestinationAlpha,
        InverseDestinationAlpha,
        DestinationColor,
        InverseDestinationColor,
        SourceAlphaSaturate,
        BlendFactor,
        InverseBlendFactor,
        Source1Color,
        InverseSource1Color,
        Source1Alpha,
        InverseSource1Alpha,
        AlphaFactor,
        InverseAlphaFactor
    }

    public enum BlendOp : byte
    {
        Add = 0,
        Subtract,
        ReverseSubtract,
        Minimum,
        Maximum,
    }

    public enum LogicOp : byte
    {
        Clear = 0,
        Set,
        Copy,
        CopyInverted,
        NoOp,
        Invert,
        And,
        Nand,
        Or,
        Nor,
        Xor,
        Equivalent,
        AndReverse,
        AndInverted,
        OrReverse,
        OrInverted
    }

    public enum InputElementSemantic : byte
    {
        Position = 0,
        Normal = 8,
        Tangent = 16,
        Bitangent = 24,
        Fog = 32,
        Color = 40,
        Texcoord = 48,
    }

    public enum InputElementFormat : byte
    {
        Padding = 0,

        Float1,
        Float2,
        Float3,
        Float4,

        UInt1,
        UInt2,
        UInt3,
        UInt4,

        Byte4
    }

    public enum InputClassification : byte
    {
        Vertex = 0,
        Instance
    }

    public enum ResourceType : byte
    {
        Texture = 0,
        ConstantBuffer,
        ShaderBuffer
    }

    public enum TextureFilter : byte
    {
        Point = 0,
        MinMagPointMipLinear,
        MinPointMagLinearMipPoint,
        MinPointMagMipLinear,
        MinLinearMagMipPoint,
        MinLinearMagPointMipLinear,
        MinMagLinearMipPoint,
        Linear,
        MinMagAnisotropicMipPoint,
    }

    public enum TextureAddressMode : byte
    {
        Repeat = 0,
        Mirror,
        ClampToEdge,
        ClampToBorder
    }
}
