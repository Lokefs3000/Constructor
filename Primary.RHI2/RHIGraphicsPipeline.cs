using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public unsafe abstract class RHIGraphicsPipeline : IDisposable, AsNativeObject<RHIGraphicsPipelineNative>
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
    }

    public struct RHIGraphicsPipelineBytecode
    {
        public Memory<byte> Vertex;
        public Memory<byte> Pixel;
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
    }

    public struct RHIGPStencilFace
    {
        public RHIStencilOperation FailOp;
        public RHIStencilOperation DepthFailOp;
        public RHIStencilOperation PassOp;
        public RHIComparisonFunction Function;
    }

    public struct RHIGPBlend
    {
        public bool AlphaToCoverageEnabled;
        public bool IndependentBlendEnabled;

        public RHIGPBlendRenderTarget[] RenderTargets;
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
    }
}
