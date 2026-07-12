using Primary.Common;
using Primary.Memory.Native;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Versioning;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe sealed class D3D12RHIGraphicsPipeline : RHIGraphicsPipeline
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<ID3D12RootSignature> _rootSignature;
        private Dictionary<D3D12RasterState, ComPtr<ID3D12PipelineState>> _createdPipelines;

        private D3D12RHIGraphicsPipelineNative* _nativeRep;

        internal D3D12RHIGraphicsPipeline(D3D12RHIDevice device, RHIGraphicsPipelineDescription description, RHIGraphicsPipelineBytecode bytecode)
        {
            _device = device;

            _bytecode = new RHIGraphicsPipelineBytecode(bytecode);
            _description = new RHIGraphicsPipelineDescription(description);

            {
                RootParameter1[] parameters = new RootParameter1[2];

                int index = 0;
                if ((!description.UseBufferForHeader && description.Header32BitConstants > 0) || (description.UseBufferForHeader && description.Expected32BitConstants > 0))
                {
                    parameters[index++] = new RootParameter1
                    {
                        ParameterType = RootParameterType.Type32BitConstants,
                        ShaderVisibility = ShaderVisibility.All,
                        Constants = new RootConstants
                        {
                            ShaderRegister = 0,
                            RegisterSpace = 0,
                            Num32BitValues = (uint)(description.UseBufferForHeader ? description.Expected32BitConstants : description.Header32BitConstants)
                        }
                    };
                }

                if (description.UseBufferForHeader && description.Header32BitConstants > 0)
                {
                    parameters[index++] = new RootParameter1
                    {
                        ParameterType = RootParameterType.TypeCbv,
                        ShaderVisibility = ShaderVisibility.All,
                        Descriptor = new RootDescriptor1
                        {
                            ShaderRegister = 1,
                            RegisterSpace = 0,
                            Flags = RootDescriptorFlags.DataStatic
                        }
                    };
                }

                StaticSamplerDesc1[] samplers = description.ImmutableSamplers.Length == 0 ?
                    Array.Empty<StaticSamplerDesc1>() :
                    new StaticSamplerDesc1[description.ImmutableSamplers.Length];

                for (int i = 0; i < samplers.Length; i++)
                {
                    RHIGPImmutableSampler @is = description.ImmutableSamplers[i];
                    samplers[i] = new StaticSamplerDesc1
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
                        ShaderVisibility = ShaderVisibility.All,
                        Flags = @is.Border >= RHISamplerBorder.OpaqueBlackUInt ? SamplerFlags.UintBorderColor : SamplerFlags.None
                    };
                }

                fixed (RootParameter1* ptr1 = parameters)
                {
                    fixed (StaticSamplerDesc1* ptr2 = samplers)
                    {
                        RootSignatureDesc2 desc = new RootSignatureDesc2
                        {
                            NumParameters = (uint)index,
                            PParameters = ptr1,
                            NumStaticSamplers = (uint)samplers.Length,
                            PStaticSamplers = ptr2,
                            Flags =
                                RootSignatureFlags.AllowInputAssemblerInputLayout |
                                RootSignatureFlags.CbvSrvUavHeapDirectlyIndexed |
                                RootSignatureFlags.SamplerHeapDirectlyIndexed
                        };

                        VersionedRootSignatureDesc versionDesc = new VersionedRootSignatureDesc(version: D3DRootSignatureVersion.Version12, desc12: desc);

                        ID3D10Blob* blob = null;
                        ID3D10Blob* error = null;

                        try
                        {
                            HResult hr = D3D12RHIDevice.D3D12.SerializeVersionedRootSignature(&versionDesc, &blob, &error);
                            if (error != null)
                            {
                                string str = new string((sbyte*)error->GetBufferPointer(), 0, (int)error->GetBufferSize());
                                throw new D3D12RHIException(str, hr.Value);
                            }

                            if (hr.IsFailure)
                            {
                                throw new D3D12RHIException($"Failed to serialize root signature blob", hr.Value);
                            }

                            hr = device.Device.CreateRootSignature(0, blob->GetBufferPointer(), blob->GetBufferSize(), out _rootSignature);
                            if (hr.IsFailure)
                            {
                                throw new D3D12RHIException($"Failed to create D3D12 root signature", hr.Value);
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

            _createdPipelines = new Dictionary<D3D12RasterState, ComPtr<ID3D12PipelineState>>();

            {
                _nativeRep = (D3D12RHIGraphicsPipelineNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIGraphicsPipelineNative>());
                _nativeRep->Base = new RHIGraphicsPipelineNative
                {

                };
            }
        }

        public ref ID3D12PipelineState GetPipelineState(D3D12RasterState rasterState)
        {
            if (_createdPipelines.TryGetValue(rasterState, out ComPtr<ID3D12PipelineState> pipeline))
                return ref pipeline.Get();

            InputElementDesc[] inputElements = _description.InputElements.Length == 0 ?
                Array.Empty<InputElementDesc>() :
                new InputElementDesc[_description.InputElements.Length];

            using MemoryScope memoryScope = ScopedMemory.PushScope();

            ScopedPtr<byte>[] elementNames = inputElements.Length == 0 ?
                Array.Empty<ScopedPtr<byte>>() :
                new ScopedPtr<byte>[inputElements.Length];

            try
            {
                for (int i = 0; i < inputElements.Length; i++)
                {
                    RHIGPInputElement ie = _description.InputElements[i];

                    string semanticStr = ie.Semantic.ToString().ToUpper();
                    ScopedPtr<byte> semanticStrPtr = ScopedMemory.Allocate((nuint)(semanticStr.Length + 1), sizeof(byte));
              
                    for (int j = 0; j < semanticStr.Length; j++)
                        semanticStrPtr[j] = (byte)semanticStr[j];
                    semanticStrPtr[semanticStr.Length] = (byte)'\0';

                    elementNames[i] = semanticStrPtr;

                    inputElements[i] = new InputElementDesc
                    {
                        SemanticName = semanticStrPtr.Pointer,
                        SemanticIndex = (uint)ie.SemanticIndex,
                        Format = ie.Format.ToFormat(),
                        InputSlot = (uint)ie.InputSlot,
                        AlignedByteOffset = (uint)ie.ByteOffset,
                        InputSlotClass = ie.InputSlotClass.ToInputClass(),
                        InstanceDataStepRate = (uint)ie.InstanceDataStepRate
                    };
                }

                fixed (InputElementDesc* ptr = inputElements)
                {
                    GraphicsPipelineStateDesc desc = new GraphicsPipelineStateDesc
                    {
                        PRootSignature = _rootSignature,

                        BlendState = new BlendDesc
                        {
                            AlphaToCoverageEnable = _description.Blend.AlphaToCoverageEnabled,
                            IndependentBlendEnable = _description.Blend.IndependentBlendEnabled
                        },
                        RasterizerState = new RasterizerDesc
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
                            ForcedSampleCount = 0,
                            ConservativeRaster = _description.Rasterizer.ConservativeRaster ?
                                ConservativeRasterizationMode.On :
                                ConservativeRasterizationMode.Off
                        },
                        DepthStencilState = new DepthStencilDesc
                        {
                            DepthEnable = _description.DepthStencil.DepthEnabled && rasterState.DSVFormat != Format.FormatUnknown,
                            DepthWriteMask = _description.DepthStencil.DepthWriteMask.ToDepthWriteMask(),
                            DepthFunc = _description.DepthStencil.DepthFunction.ToComparisonFunc(),
                            StencilEnable = _description.DepthStencil.StencilEnabled,
                            StencilReadMask = _description.DepthStencil.StencilReadMask,
                            StencilWriteMask = _description.DepthStencil.StencilWriteMask,
                            FrontFace = new DepthStencilopDesc
                            {
                                StencilFailOp = _description.DepthStencil.FrontFace.FailOp.ToStencilOp(),
                                StencilDepthFailOp = _description.DepthStencil.FrontFace.DepthFailOp.ToStencilOp(),
                                StencilPassOp = _description.DepthStencil.FrontFace.PassOp.ToStencilOp(),
                                StencilFunc = _description.DepthStencil.FrontFace.Function.ToComparisonFunc(),
                            },
                            BackFace = new DepthStencilopDesc
                            {
                                StencilFailOp = _description.DepthStencil.BackFace.FailOp.ToStencilOp(),
                                StencilDepthFailOp = _description.DepthStencil.BackFace.DepthFailOp.ToStencilOp(),
                                StencilPassOp = _description.DepthStencil.BackFace.PassOp.ToStencilOp(),
                                StencilFunc = _description.DepthStencil.BackFace.Function.ToComparisonFunc(),
                            }
                        },
                        InputLayout = new InputLayoutDesc
                        {
                            PInputElementDescs = ptr,
                            NumElements = (uint)inputElements.Length
                        },

                        PrimitiveTopologyType = _description.PrimitiveTopologyType.ToPrimitiveTopologyType(),

                        SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                        SampleMask = uint.MaxValue,

                        CachedPSO = new CachedPipelineState { CachedBlobSizeInBytes = 0, PCachedBlob = null },
                        Flags = PipelineStateFlags.None,
                    };

                    for (int i = 0; i < _description.Blend.RenderTargets.Length; i++)
                    {
                        RHIGPBlendRenderTarget rt = _description.Blend.RenderTargets[i];
                        desc.BlendState.RenderTarget[i] = new RenderTargetBlendDesc
                        {
                            BlendEnable = rt.BlendEnabled,
                            LogicOpEnable = false,
                            SrcBlend = rt.SourceBlend.ToBlend(),
                            DestBlend = rt.DestinationBlend.ToBlend(),
                            BlendOp = rt.BlendOperation.ToBlendOp(),
                            SrcBlendAlpha = rt.SourceBlendAlpha.ToBlend(),
                            DestBlendAlpha = rt.DestinationBlendAlpha.ToBlend(),
                            BlendOpAlpha = rt.BlendOperationAlpha.ToBlendOp(),
                            LogicOp = LogicOp.Noop,
                            RenderTargetWriteMask = rt.WriteMask
                        };
                    }

                    desc.NumRenderTargets = (uint)rasterState.RTVFormats.Count;
                    desc.DSVFormat = rasterState.DSVFormat;

                    NativeMemory.Copy(&rasterState.RTVFormats, &desc.RTVFormats.Element0, (nuint)(Unsafe.SizeOf<Format>() * 8));

                    using MemoryHandle vertexBc = _bytecode.Vertex.Pin();
                    desc.VS = new ShaderBytecode
                    {
                        PShaderBytecode = vertexBc.Pointer,
                        BytecodeLength = (nuint)_bytecode.Vertex.Length
                    };

                    using MemoryHandle pixelBc = _bytecode.Pixel.Pin();
                    desc.PS = new ShaderBytecode
                    {
                        PShaderBytecode = pixelBc.Pointer,
                        BytecodeLength = (nuint)_bytecode.Pixel.Length
                    };

                    HResult hr = _device.Device.CreateGraphicsPipelineState(&desc, out pipeline);
                    if (hr.IsFailure)
                    {
                        _createdPipelines[rasterState] = null;
                        return ref Unsafe.NullRef<ID3D12PipelineState>();
                    }

                    if (_debugName != null)
                    {
                        ResourceHelper.SetResourceName(ref pipeline.Get(), $"{_debugName}-{rasterState.GetHashCode()}");
                    }

                    _createdPipelines[rasterState] = pipeline;
                    return ref pipeline.Get();
                }
            }
            finally
            {
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.AddResourceFreeNextFrame(() =>
                {
                    if (_nativeRep != null)
                        NativeMemory.Free(_nativeRep);
                    _nativeRep = null;

                    foreach (var kvp in _createdPipelines)
                        kvp.Value.Dispose();
                    _createdPipelines.Clear();

                    _rootSignature.Dispose();

                    _device.ResourceTracker.Untrack(this);
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (debugName != null)
            {
                if (!Unsafe.IsNullRef(in _rootSignature.Get()))
                {
                    ResourceHelper.SetResourceName(ref _rootSignature.Get(), $"{debugName}-RootSig");
                }

                foreach (var kvp in _createdPipelines)
                {
                    ResourceHelper.SetResourceName(ref kvp.Value.Get(), $"{debugName}-{kvp.Key.GetHashCode()}");
                }
            }
        }

        public override string ToString()
        {
            return $"RHIGraphicsPipeline{{{_debugName}}}";
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
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct D3D12RasterState : IEquatable<D3D12RasterState>
    {
        public __RTVs RTVFormats;
        public Format DSVFormat;

        public D3D12RasterState()
        {
            DSVFormat = Format.FormatUnknown;
            RTVFormats = new __RTVs();
        }

        public bool Equals(D3D12RasterState other)
        {
            return
                RTVFormats.Count == other.RTVFormats.Count &&
                DSVFormat == other.DSVFormat &&
                Vector256.LoadUnsafe(ref Unsafe.As<D3D12RasterState, uint>(ref this)) == Vector256.LoadUnsafe(ref Unsafe.As<D3D12RasterState, uint>(ref this));

        }

        public struct __RTVs
        {
            public Format e0;
            public Format e1;
            public Format e2;
            public Format e3;
            public Format e4;
            public Format e5;
            public Format e6;
            public Format e7;

            public int Count;

            public __RTVs()
            {
                e0 = Format.FormatUnknown;
                e1 = Format.FormatUnknown;
                e2 = Format.FormatUnknown;
                e3 = Format.FormatUnknown;
                e4 = Format.FormatUnknown;
                e5 = Format.FormatUnknown;
                e6 = Format.FormatUnknown;
                e7 = Format.FormatUnknown;
            }

            public Format this[int index]
            {
                get => Unsafe.Add(ref e0, index);
                set => Unsafe.Add(ref e0, index) = value;
            }
        }
    }
}
