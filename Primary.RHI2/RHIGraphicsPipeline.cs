namespace Primary.RHI2
{
    public unsafe abstract class RHIGraphicsPipeline : IDisposable, IAsNativeObject<RHIGraphicsPipelineNative>
    {
        protected RHIGraphicsPipelineDescription _description;
        protected RHIGraphicsPipelineBytecode _bytecode;

        protected string? _debugName;

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);
        protected abstract void SetDebugName(string? debugName);

        ~RHIGraphicsPipeline()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public string? DebugName
        {
            get => _debugName;
            set
            {
                if (_debugName != value)
                    SetDebugName(_debugName);
                _debugName = value;
            }
        }

        public ref readonly RHIGraphicsPipelineDescription Description => ref _description;
        public ref readonly RHIGraphicsPipelineBytecode Bytecode => ref _bytecode;

        public abstract RHIGraphicsPipelineNative* GetAsNative();
    }

    public struct RHIGraphicsPipelineNative
    {

    }

    public struct RHIGraphicsPipelineDescription
    {
        public RHIGPRasterizer Rasterizer;
        public RHIGPDepthStencil DepthStencil;
        public RHIGPBlend Blend;
        public RHIGPInputElement[] InputElements;
        public RHIGPImmutableSampler[] ImmutableSamplers;

        public RHIPrimitiveTopologyType PrimitiveTopologyType;

        public int Expected32BitConstants;

        public int Header32BitConstants;
        public bool UseBufferForHeader;

        public RHIGraphicsPipelineDescription()
        {
            Rasterizer = new RHIGPRasterizer();
            DepthStencil = new RHIGPDepthStencil();
            Blend = new RHIGPBlend();
            InputElements = Array.Empty<RHIGPInputElement>();
            ImmutableSamplers = Array.Empty<RHIGPImmutableSampler>();

            PrimitiveTopologyType = RHIPrimitiveTopologyType.Triangle;

            Expected32BitConstants = 0;

            Header32BitConstants = 0;
            UseBufferForHeader = false;
        }

        public RHIGraphicsPipelineDescription(RHIGraphicsPipelineDescription other)
        {
            Rasterizer = new RHIGPRasterizer(other.Rasterizer);
            DepthStencil = new RHIGPDepthStencil(other.DepthStencil);
            Blend = new RHIGPBlend(other.Blend);
            InputElements = (RHIGPInputElement[])other.InputElements.Clone();
            ImmutableSamplers = (RHIGPImmutableSampler[])other.ImmutableSamplers.Clone();

            PrimitiveTopologyType = other.PrimitiveTopologyType;

            Expected32BitConstants = other.Expected32BitConstants;

            Header32BitConstants = other.Header32BitConstants;
            UseBufferForHeader = other.UseBufferForHeader;
        }
    }

    public struct RHIGraphicsPipelineBytecode
    {
        public Memory<byte> Vertex;
        public Memory<byte> Pixel;

        public RHIGraphicsPipelineBytecode()
        {
            Vertex = Memory<byte>.Empty;
            Pixel = Memory<byte>.Empty;
        }

        public RHIGraphicsPipelineBytecode(RHIGraphicsPipelineBytecode other)
        {
            Vertex = other.Vertex.ToArray();
            Pixel = other.Pixel.ToArray();
        }
    }

    public struct RHIGPRasterizer
    {
        public RHIFillMode Fill;
        public RHICullMode Cull;
        public bool FrontCounterClockwise;

        public int DepthBias;
        public float DepthBiasClamp;
        public float SlopeScaledDepthBias;

        public bool DepthClipEnabled;

        public bool ConservativeRaster;

        public RHIGPRasterizer()
        {
            Fill = RHIFillMode.Solid;
            Cull = RHICullMode.Back;
            FrontCounterClockwise = false;

            DepthBias = 0;
            DepthBiasClamp = 0.0f;
            SlopeScaledDepthBias = 0.0f;

            DepthClipEnabled = true;

            ConservativeRaster = false;
        }

        public RHIGPRasterizer(RHIGPRasterizer other)
        {
            Fill = other.Fill;
            Cull = other.Cull;
            FrontCounterClockwise = other.FrontCounterClockwise;

            DepthBias = other.DepthBias;
            DepthBiasClamp = other.DepthBiasClamp;
            SlopeScaledDepthBias = other.SlopeScaledDepthBias;

            DepthClipEnabled = other.DepthClipEnabled;

            ConservativeRaster = other.ConservativeRaster;
        }
    }

    public struct RHIGPDepthStencil
    {
        public bool DepthEnabled;

        public RHIDepthWriteMask DepthWriteMask;
        public RHIComparisonFunction DepthFunction;

        public bool StencilEnabled;
        public byte StencilReadMask;
        public byte StencilWriteMask;

        public RHIGPStencilFace FrontFace;
        public RHIGPStencilFace BackFace;

        public RHIGPDepthStencil()
        {
            DepthEnabled = false;

            DepthWriteMask = RHIDepthWriteMask.All;
            DepthFunction = RHIComparisonFunction.LessEqual;

            StencilEnabled = false;
            StencilReadMask = 0xff;
            StencilWriteMask = 0xff;

            FrontFace = new RHIGPStencilFace();
            BackFace = new RHIGPStencilFace();
        }

        public RHIGPDepthStencil(RHIGPDepthStencil other)
        {
            DepthEnabled = other.DepthEnabled;

            DepthWriteMask = other.DepthWriteMask;
            DepthFunction = other.DepthFunction;

            StencilEnabled = other.StencilEnabled;
            StencilReadMask = other.StencilReadMask;
            StencilWriteMask = other.StencilWriteMask;

            FrontFace = other.FrontFace;
            BackFace = other.BackFace;
        }
    }

    public struct RHIGPStencilFace
    {
        public RHIStencilOperation FailOp;
        public RHIStencilOperation DepthFailOp;
        public RHIStencilOperation PassOp;
        public RHIComparisonFunction Function;

        public RHIGPStencilFace()
        {
            FailOp = RHIStencilOperation.Keep;
            DepthFailOp = RHIStencilOperation.Keep;
            PassOp = RHIStencilOperation.Keep;
            Function = RHIComparisonFunction.Never;
        }

        public RHIGPStencilFace(RHIGPStencilFace other)
        {
            FailOp = other.FailOp;
            DepthFailOp = other.DepthFailOp;
            PassOp = other.PassOp;
            Function = other.Function;
        }
    }

    public struct RHIGPBlend
    {
        public bool AlphaToCoverageEnabled;
        public bool IndependentBlendEnabled;

        public RHIGPBlendRenderTarget[] RenderTargets;

        public RHIGPBlend()
        {
            AlphaToCoverageEnabled = false;
            IndependentBlendEnabled = false;

            RenderTargets = Array.Empty<RHIGPBlendRenderTarget>();
        }

        public RHIGPBlend(RHIGPBlend other)
        {
            AlphaToCoverageEnabled = other.AlphaToCoverageEnabled;
            IndependentBlendEnabled = other.IndependentBlendEnabled;

            RenderTargets = (RHIGPBlendRenderTarget[])other.RenderTargets.Clone();
        }
    }

    public struct RHIGPBlendRenderTarget
    {
        public bool BlendEnabled;

        public RHIBlend SourceBlend;
        public RHIBlend DestinationBlend;
        public RHIBlendOperation BlendOperation;

        public RHIBlend SourceBlendAlpha;
        public RHIBlend DestinationBlendAlpha;
        public RHIBlendOperation BlendOperationAlpha;

        public byte WriteMask;

        public RHIGPBlendRenderTarget()
        {
            BlendEnabled = false;

            SourceBlend = RHIBlend.One;
            DestinationBlend = RHIBlend.Zero;
            BlendOperation = RHIBlendOperation.Add;

            SourceBlendAlpha = RHIBlend.One;
            DestinationBlendAlpha = RHIBlend.Zero;
            BlendOperationAlpha = RHIBlendOperation.Add;

            WriteMask = 0xf;
        }

        public RHIGPBlendRenderTarget(RHIGPBlendRenderTarget other)
        {
            BlendEnabled = other.BlendEnabled;

            SourceBlend = other.SourceBlend;
            DestinationBlend = other.DestinationBlend;
            BlendOperation = other.BlendOperation;

            SourceBlendAlpha = other.SourceBlendAlpha;
            DestinationBlendAlpha = other.DestinationBlendAlpha;
            BlendOperationAlpha = other.BlendOperationAlpha;

            WriteMask = other.WriteMask;
        }
    }

    public struct RHIGPInputElement
    {
        public RHIElementSemantic Semantic;
        public int SemanticIndex;

        public RHIElementFormat Format;

        public int InputSlot;
        public int ByteOffset;

        public RHIInputClass InputSlotClass;
        public int InstanceDataStepRate;

        public RHIGPInputElement()
        {
            Semantic = RHIElementSemantic.Position;
            SemanticIndex = 0;

            Format = RHIElementFormat.Single1;

            InputSlot = 0;
            ByteOffset = 0;

            InputSlotClass = RHIInputClass.PerVertex;
            InstanceDataStepRate = 1;
        }

        public RHIGPInputElement(RHIGPInputElement other)
        {
            Semantic = other.Semantic;
            SemanticIndex = other.SemanticIndex;

            Format = other.Format;

            InputSlot = other.InputSlot;
            ByteOffset = other.ByteOffset;

            InputSlotClass = other.InputSlotClass;
            InstanceDataStepRate = other.InstanceDataStepRate;
        }
    }

    public struct RHIGPImmutableSampler
    {
        public RHIFilterType Min;
        public RHIFilterType Mag;
        public RHIFilterType Mip;
        public RHIReductionType ReductionType;

        public RHITextureAddressMode AddressModeU;
        public RHITextureAddressMode AddressModeV;
        public RHITextureAddressMode AddressModeW;

        public float MipLODBias;
        public uint MaxAnisotropy;

        public RHIComparisonFunction ComparisonFunction;

        public RHISamplerBorder Border;

        public float MinLOD;
        public float MaxLOD;

        public RHIGPImmutableSampler()
        {
            Min = RHIFilterType.Linear;
            Mag = RHIFilterType.Linear;
            Mip = RHIFilterType.Linear;
            ReductionType = RHIReductionType.Standard;

            AddressModeU = RHITextureAddressMode.Repeat;
            AddressModeV = RHITextureAddressMode.Repeat;
            AddressModeW = RHITextureAddressMode.Repeat;

            MipLODBias = 1.0f;
            MaxAnisotropy = 1;

            ComparisonFunction = RHIComparisonFunction.Never;

            Border = RHISamplerBorder.TransparentBlack;

            MinLOD = 0.0f;
            MaxLOD = float.MaxValue;
        }

        public RHIGPImmutableSampler(RHIGPImmutableSampler other)
        {
            Min = other.Min;
            Mag = other.Mag;
            ReductionType = other.ReductionType;

            AddressModeU = other.AddressModeU;
            AddressModeV = other.AddressModeV;
            AddressModeW = other.AddressModeW;

            MipLODBias = other.MipLODBias;
            MaxAnisotropy = other.MaxAnisotropy;

            ComparisonFunction = other.ComparisonFunction;

            Border = other.Border;

            MinLOD = other.MinLOD;
            MaxLOD = other.MaxLOD;
        }
    }
}
