using Primary.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using CommunityToolkit.HighPerformance;
using System.Buffers;

using static TerraFX.Interop.DirectX.D3D12_PIPELINE_STATE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
using static TerraFX.Interop.DirectX.D3D12_LOGIC_OP;
using static TerraFX.Interop.DirectX.D3D12_ROOT_PARAMETER_TYPE;
using static TerraFX.Interop.DirectX.D3D12_SHADER_VISIBILITY;
using static TerraFX.Interop.DirectX.D3D12_ROOT_SIGNATURE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_ROOT_DESCRIPTOR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_SAMPLER_FLAGS;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    public unsafe sealed class D3D12RHIGraphicsPipeline : RHIGraphicsPipeline
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<ID3D12RootSignature> _rootSignature;
        private ConcurrentDictionary<D3D12RasterState, ComPtr<ID3D12PipelineState>> _createdPipelines;

        private D3D12RHIGraphicsPipelineNative* _nativeRep;

        internal D3D12RHIGraphicsPipeline(D3D12RHIDevice device, RHIGraphicsPipelineDescription description, RHIGraphicsPipelineBytecode bytecode)
        {
            _device = device;
            _description = description;

            {
                D3D12_ROOT_PARAMETER1[] parameters = new D3D12_ROOT_PARAMETER1[2];

                int index = 0;
                if ((!description.UseBufferForHeader && description.Header32BitConstants > 0) || (description.UseBufferForHeader && description.Expected32BitConstants > 0))
                {
                    parameters[index++] = new D3D12_ROOT_PARAMETER1
                    {
                        ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS,
                        ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
                        Constants = new D3D12_ROOT_CONSTANTS
                        {
                            ShaderRegister = 0,
                            RegisterSpace = 0,
                            Num32BitValues = (uint)(description.UseBufferForHeader ? description.Expected32BitConstants : description.Header32BitConstants)
                        }
                    };
                }

                if (description.UseBufferForHeader && description.Header32BitConstants > 0)
                {
                    parameters[index++] = new D3D12_ROOT_PARAMETER1
                    {
                        ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
                        ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
                        Descriptor = new D3D12_ROOT_DESCRIPTOR1
                        {
                            ShaderRegister = 1,
                            RegisterSpace = 0,
                            Flags = D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC
                        }
                    };
                }

                D3D12_STATIC_SAMPLER_DESC1[] samplers = description.ImmutableSamplers.Length > 0 ?
                    Array.Empty<D3D12_STATIC_SAMPLER_DESC1>() :
                    new D3D12_STATIC_SAMPLER_DESC1[description.ImmutableSamplers.Length];

                for (int i = 0; i < samplers.Length; i++)
                {
                    RHIGPImmutableSampler @is = description.ImmutableSamplers[i];
                    samplers[i] = new D3D12_STATIC_SAMPLER_DESC1
                    {
                        Filter = @is.MaxAnisotropy > 1 ?
                            ResourceHelper.EncodeAnisotropicFilter(@is.ReductionType) :
                            ResourceHelper.EncodeBasicFilter(@is.Min, @is.Mag, @is.Mip, @is.ReductionType),
                        AddressU = @is.AddressModeU.ToTextureAddressMode(),
                        AddressV = @is.AddressModeV.ToTextureAddressMode(),
                        AddressW = @is.AddressModeW.ToTextureAddressMode(),
                        MipLODBias = @is.MipLODBias,
                        MaxAnisotropy = @is.MaxAnisotropy,
                        ComparisonFunc = @is.ComparisonFunction.ToComparisonFunc(),
                        BorderColor = @is.Border.ToStaticBorderColor(),
                        MinLOD = @is.MinLOD,
                        MaxLOD = @is.MaxLOD,
                        ShaderRegister = (uint)i,
                        RegisterSpace = 0,
                        ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
                        Flags = @is.Border >= RHISamplerBorder.OpaqueBlackUInt ? D3D12_SAMPLER_FLAG_UINT_BORDER_COLOR : D3D12_SAMPLER_FLAG_NONE
                    };
                }

                fixed (D3D12_ROOT_PARAMETER1* ptr1 = parameters)
                {
                    fixed (D3D12_STATIC_SAMPLER_DESC1* ptr2 = samplers)
                    {
                        D3D12_ROOT_SIGNATURE_DESC2 desc = new D3D12_ROOT_SIGNATURE_DESC2
                        {
                            NumParameters = (uint)index,
                            pParameters = ptr1,
                            NumStaticSamplers = (uint)samplers.Length,
                            pStaticSamplers = ptr2,
                            Flags =
                                D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
                                D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED |
                                D3D12_ROOT_SIGNATURE_FLAG_SAMPLER_HEAP_DIRECTLY_INDEXED
                        };

                        D3D12_VERSIONED_ROOT_SIGNATURE_DESC versionDesc = new D3D12_VERSIONED_ROOT_SIGNATURE_DESC(desc);

                        ID3DBlob* blob = null;
                        ID3DBlob* error = null;

                        try
                        {
                            HRESULT hr = DirectX.D3D12SerializeVersionedRootSignature(&versionDesc, &blob, &error);
                            if (error != null)
                            {
                                string str = new string((sbyte*)error->GetBufferPointer(), 0, (int)error->GetBufferSize());
                                throw new RHIException(str);
                            }

                            if (hr.FAILED)
                            {
                                throw new RHIException($"Failed to serialize root signature blob: {hr}");
                            }

                            hr = device.Device.Get()->CreateRootSignature(0, blob->GetBufferPointer(), blob->GetBufferSize(), UuidOf.Get<ID3D12RootSignature>(), (void**)_rootSignature.GetAddressOf());
                            if (hr.FAILED)
                            {
                                throw new RHIException($"Failed to create D3D12 root signature: {hr}");
                            }
                        }
                        finally
                        {
                            if (blob != null)
                                blob->Release();
                            if (error != null)
                                error->Release();
                        }
                    }
                }
            }

            _createdPipelines = new ConcurrentDictionary<D3D12RasterState, ComPtr<ID3D12PipelineState>>();

            {
                _nativeRep = (D3D12RHIGraphicsPipelineNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIGraphicsPipelineNative>());
                _nativeRep->Base = new RHIGraphicsPipelineNative
                {

                };
            }
        }

        public ID3D12PipelineState* GetPipelineState(D3D12RasterState rasterState)
        {
            if (_createdPipelines.TryGetValue(rasterState, out ComPtr<ID3D12PipelineState> pipeline))
                return pipeline.Get();

            D3D12_INPUT_ELEMENT_DESC[] inputElements = _description.InputElements.Length == 0 ?
                Array.Empty<D3D12_INPUT_ELEMENT_DESC>() :
                new D3D12_INPUT_ELEMENT_DESC[_description.InputElements.Length];

            Ptr<sbyte>[] elementNames = inputElements.Length == 0 ?
                Array.Empty<Ptr<sbyte>>() :
                new Ptr<sbyte>[inputElements.Length];

            try
            {
                for (int i = 0; i < inputElements.Length; i++)
                {
                    RHIGPInputElement ie = _description.InputElements[i];

                    string semanticStr = ie.Semantic.ToString().ToUpper();
                    sbyte* semanticStrPtr = (sbyte*)NativeMemory.Alloc((nuint)(semanticStr.Length + 1), sizeof(sbyte));

                    NativeMemory.Copy(Unsafe.AsPointer(ref semanticStr.DangerousGetReference()), semanticStrPtr, (nuint)semanticStr.Length);
                    semanticStrPtr[semanticStr.Length] = (sbyte)'\0';

                    elementNames[i] = semanticStrPtr;

                    inputElements[i] = new D3D12_INPUT_ELEMENT_DESC
                    {
                        SemanticName = semanticStrPtr,
                        SemanticIndex = (uint)ie.SemanticIndex,
                        Format = ie.Format.ToFormat(),
                        InputSlot = (uint)ie.InputSlot,
                        AlignedByteOffset = (uint)ie.ByteOffset,
                        InputSlotClass = ie.InputSlotClass.ToInputClass(),
                        InstanceDataStepRate = (uint)ie.InstanceDataStepRate
                    };
                }

                fixed (D3D12_INPUT_ELEMENT_DESC* ptr = inputElements)
                {
                    D3D12_GRAPHICS_PIPELINE_STATE_DESC desc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC
                    {
                        pRootSignature = _rootSignature,

                        BlendState = new D3D12_BLEND_DESC
                        {
                            AlphaToCoverageEnable = _description.Blend.AlphaToCoverageEnabled,
                            IndependentBlendEnable = _description.Blend.IndependentBlendEnabled
                        },
                        RasterizerState = new D3D12_RASTERIZER_DESC
                        {
                            FillMode = _description.Rasterizer.Fill.ToFillMode(),
                            CullMode = _description.Rasterizer.Cull.ToCullMode(),
                            FrontCounterClockwise = _description.Rasterizer.FrontCounterClockwise,
                            DepthBias = _description.Rasterizer.DepthBias,
                            DepthBiasClamp = _description.Rasterizer.DepthBiasClamp,
                            SlopeScaledDepthBias = _description.Rasterizer.SlopeScaledDepthBias,
                            DepthClipEnable = _description.Rasterizer.DepthClipEnabled,
                            MultisampleEnable = false,
                            AntialiasedLineEnable = false,
                            ForcedSampleCount = 1,
                            ConservativeRaster = _description.Rasterizer.ConservativeRaster ?
                                D3D12_CONSERVATIVE_RASTERIZATION_MODE_ON :
                                D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF
                        },
                        DepthStencilState = new D3D12_DEPTH_STENCIL_DESC
                        {
                            DepthEnable = _description.DepthStencil.DepthEnabled,
                            DepthWriteMask = _description.DepthStencil.DepthWriteMask.ToDepthWriteMask(),
                            DepthFunc = _description.DepthStencil.DepthFunction.ToComparisonFunc(),
                            StencilEnable = _description.DepthStencil.StencilEnabled,
                            StencilReadMask = _description.DepthStencil.StencilReadMask,
                            StencilWriteMask = _description.DepthStencil.StencilWriteMask,
                            FrontFace = new D3D12_DEPTH_STENCILOP_DESC
                            {
                                StencilFailOp = _description.DepthStencil.FrontFace.FailOp.ToStencilOp(),
                                StencilDepthFailOp = _description.DepthStencil.FrontFace.FailOp.ToStencilOp(),
                                StencilPassOp = _description.DepthStencil.FrontFace.FailOp.ToStencilOp(),
                                StencilFunc = _description.DepthStencil.FrontFace.Function.ToComparisonFunc(),
                            },
                            BackFace = new D3D12_DEPTH_STENCILOP_DESC
                            {
                                StencilFailOp = _description.DepthStencil.BackFace.FailOp.ToStencilOp(),
                                StencilDepthFailOp = _description.DepthStencil.BackFace.FailOp.ToStencilOp(),
                                StencilPassOp = _description.DepthStencil.BackFace.FailOp.ToStencilOp(),
                                StencilFunc = _description.DepthStencil.BackFace.Function.ToComparisonFunc(),
                            }
                        },
                        InputLayout = new D3D12_INPUT_LAYOUT_DESC
                        {
                            pInputElementDescs = ptr,
                            NumElements = (uint)inputElements.Length
                        },

                        PrimitiveTopologyType = _description.PrimitiveTopologyType.ToPrimitiveTopologyType(),

                        SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                        CachedPSO = new D3D12_CACHED_PIPELINE_STATE { CachedBlobSizeInBytes = 0, pCachedBlob = null },
                        Flags = D3D12_PIPELINE_STATE_FLAG_NONE
                    };

                    for (int i = 0; i < _description.Blend.RenderTargets.Length; i++)
                    {
                        RHIGPBlendRenderTarget rt = _description.Blend.RenderTargets[i];
                        desc.BlendState.RenderTarget[i] = new D3D12_RENDER_TARGET_BLEND_DESC
                        {
                            BlendEnable = rt.BlendEnabled,
                            LogicOpEnable = false,
                            SrcBlend = rt.SourceBlend.ToBlend(),
                            DestBlend = rt.DestinationBlend.ToBlend(),
                            BlendOp = rt.BlendOperation.ToBlendOp(),
                            SrcBlendAlpha = rt.SourceBlendAlpha.ToBlend(),
                            DestBlendAlpha = rt.DestinationBlendAlpha.ToBlend(),
                            BlendOpAlpha = rt.BlendOperationAlpha.ToBlendOp(),
                            LogicOp = D3D12_LOGIC_OP_NOOP,
                            RenderTargetWriteMask = rt.WriteMask
                        };
                    }

                    desc.NumRenderTargets = (uint)rasterState.RTVFormats.Count;
                    desc.DSVFormat = rasterState.DSVFormat;

                    NativeMemory.Copy(&rasterState.RTVFormats, &desc.RTVFormats.e0, (nuint)(Unsafe.SizeOf<DXGI_FORMAT>() * 8));

                    using MemoryHandle vertexBc = _bytecode.Vertex.Pin();
                    desc.VS = new D3D12_SHADER_BYTECODE
                    {
                        pShaderBytecode = vertexBc.Pointer,
                        BytecodeLength = (nuint)_bytecode.Vertex.Length
                    };

                    using MemoryHandle pixelBc = _bytecode.Pixel.Pin();
                    desc.PS = new D3D12_SHADER_BYTECODE
                    {
                        pShaderBytecode = pixelBc.Pointer,
                        BytecodeLength = (nuint)_bytecode.Pixel.Length
                    };

                    HRESULT hr = _device.Device.Get()->CreateGraphicsPipelineState(&desc, UuidOf.Get<ID3D12PipelineState>(), (void**)pipeline.GetAddressOf());
                    if (hr.FAILED)
                    {
                        _createdPipelines[rasterState] = null;
                        return null;
                    }

                    if (_debugName != null)
                    {
                        ResourceHelper.SetResourceName((ID3D12Resource2*)pipeline.Get(), $"{_debugName}-{rasterState.GetHashCode()}");
                    }

                    _createdPipelines[rasterState] = pipeline;
                    return pipeline.Get();
                }
            }
            finally
            {
                for (int i = 0; i < elementNames.Length; i++)
                {
                    if (!elementNames[i].IsNull)
                        NativeMemory.Free(elementNames[i].Pointer);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _device.AddResourceFreeNextFrame(() =>
                {
                    if (_nativeRep != null)
                        NativeMemory.Free(_nativeRep);
                    _nativeRep = null;

                    foreach (var kvp in _createdPipelines)
                        kvp.Value.Reset();
                    _createdPipelines.Clear();

                    _rootSignature.Reset();
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (debugName != null)
            {
                if (_rootSignature.Get() != null)
                {
                    ResourceHelper.SetResourceName((ID3D12Resource2*)_rootSignature.Get(), $"{debugName}-RootSig");
                }

                foreach (var kvp in _createdPipelines)
                {
                    ResourceHelper.SetResourceName((ID3D12Resource2*)kvp.Value.Get(), $"{debugName}-{kvp.Key.GetHashCode()}");
                }
            }
        }

        public override unsafe RHIGraphicsPipelineNative* GetAsNative() => (RHIGraphicsPipelineNative*)_nativeRep;

        public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
    }

    public unsafe struct D3D12RHIGraphicsPipelineNative
    {
        public RHIGraphicsPipelineNative Base;

        public static implicit operator RHIGraphicsPipelineNative(D3D12RHIGraphicsPipelineNative native) => native.Base;
    }

    [SupportedOSPlatform("windows")]
    public struct D3D12RasterState
    {
        public DXGI_FORMAT DSVFormat;
        public __RTVs RTVFormats;

        public struct __RTVs
        {
            public int Count;

            public DXGI_FORMAT e0;
            public DXGI_FORMAT e1;
            public DXGI_FORMAT e2;
            public DXGI_FORMAT e3;
            public DXGI_FORMAT e4;
            public DXGI_FORMAT e5;
            public DXGI_FORMAT e6;
            public DXGI_FORMAT e7;

            public DXGI_FORMAT this[int index]
            {
                get => Unsafe.Add(ref e0, index);
                set => Unsafe.Add(ref e0, index) = value;
            }
        }
    }
}
