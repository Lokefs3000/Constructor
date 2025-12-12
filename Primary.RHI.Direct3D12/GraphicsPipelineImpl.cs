using Primary.Common;
using Primary.RHI.Direct3D12.Utility;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Primary.RHI.Direct3D12
{
    internal sealed unsafe class GraphicsPipelineImpl : GraphicsPipeline
    {
        private readonly GraphicsDeviceImpl _device;
        private GraphicsPipelineDescription _description;
        private GraphicsPipelineBytecode _bytecode;

        private ConcurrentDictionary<int, ID3D12PipelineState?> _pipelineStates;
        private ID3D12RootSignature _rootSignature;
        private GraphicsPipelineStateDescription _stateDescription;

        private Format[] _rtvFormatArray;
        private bool _needsConstantBuffer;

        private bool _disposedValue;

        internal GraphicsPipelineImpl(GraphicsDeviceImpl device, GraphicsPipelineDescription desc, GraphicsPipelineBytecode bytecode)
        {
            _device = device;
            _pipelineStates = new ConcurrentDictionary<int, ID3D12PipelineState?>();
            _description = CloneDescription(ref desc);
            _bytecode = CloneBytecode(ref bytecode);

            _rtvFormatArray = [Format.Unknown, Format.Unknown, Format.Unknown, Format.Unknown, Format.Unknown, Format.Unknown, Format.Unknown, Format.Unknown];

            _needsConstantBuffer = ((desc.BoundResources.Length * sizeof(uint)) + desc.ExpectedConstantsSize) > 128;
            int requiredParameters = 1 + (desc.ExpectedConstantsSize > 0 && _needsConstantBuffer ? 1 : 0);

            RootSignatureDescription2 signatureDesc;

            if (desc.Num32BitValues == 0 && desc.BoundResources.Length == 0 && desc.ExpectedConstantsSize == 0)
            {
                signatureDesc = new RootSignatureDescription2
                {
                    Flags = RootSignatureFlags.AllowInputAssemblerInputLayout,
                    Parameters = Array.Empty<RootParameter1>(),
                    StaticSamplers = desc.ImmutableSamplers.Length > 0 ? new StaticSamplerDescription1[desc.ImmutableSamplers.Length] : Array.Empty<StaticSamplerDescription1>()
                };
            }
            else
            {
                if (desc.Num32BitValues == 0)
                {
                    signatureDesc = new RootSignatureDescription2
                    {
                        Flags = RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed | RootSignatureFlags.AllowInputAssemblerInputLayout,
                        Parameters = new RootParameter1[requiredParameters],
                        StaticSamplers = desc.ImmutableSamplers.Length > 0 ? new StaticSamplerDescription1[desc.ImmutableSamplers.Length] : Array.Empty<StaticSamplerDescription1>()
                    };

                    if (_needsConstantBuffer)
                        signatureDesc.Parameters[0] = new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(0u, 0u, RootDescriptorFlags.DataStaticWhileSetAtExecute), ShaderVisibility.All);
                    else
                        signatureDesc.Parameters[0] = new RootParameter1(new RootConstants(0u, 0u, (uint)(desc.BoundResources.Length * sizeof(uint)) + desc.ExpectedConstantsSize), ShaderVisibility.All);
                    if (desc.ExpectedConstantsSize > 0 && _needsConstantBuffer)
                        signatureDesc.Parameters[1] = new RootParameter1(new RootConstants(1u, 0u, desc.ExpectedConstantsSize), ShaderVisibility.All);
                }
                else
                {
                    signatureDesc = new RootSignatureDescription2
                    {
                        Flags = RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed | RootSignatureFlags.AllowInputAssemblerInputLayout,
                        Parameters = new RootParameter1[desc.HasConstantBuffer ? 2 : 1],
                        StaticSamplers = desc.ImmutableSamplers.Length > 0 ? new StaticSamplerDescription1[desc.ImmutableSamplers.Length] : Array.Empty<StaticSamplerDescription1>()
                    };

                    signatureDesc.Parameters[0] = new RootParameter1(new RootConstants(0u, 0u, (uint)(desc.Num32BitValues * sizeof(uint))), ShaderVisibility.All);
                    if (desc.HasConstantBuffer)
                        signatureDesc.Parameters[1] = new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(1u, 0u, RootDescriptorFlags.DataStaticWhileSetAtExecute), ShaderVisibility.All);
                }
            }

            for (int i = 0; i < desc.ImmutableSamplers.Length; i++)
            {
                ref KeyValuePair<uint, ImmutableSamplerDescription> sampler = ref desc.ImmutableSamplers[i];
                ImmutableSamplerDescription samplerDescription = sampler.Value;

                signatureDesc.StaticSamplers[i] = new StaticSamplerDescription1
                {
                    Filter = samplerDescription.MaxAnistropy > 1 ? Filter.Anisotropic : SwitchFilter(samplerDescription.Filter),
                    AddressU = SwitchAddressMode(samplerDescription.AddressModeU),
                    AddressV = SwitchAddressMode(samplerDescription.AddressModeV),
                    AddressW = SwitchAddressMode(samplerDescription.AddressModeW),
                    MipLODBias = 0.0f,
                    MaxAnisotropy = Math.Max(samplerDescription.MaxAnistropy, 1u),
                    ComparisonFunction = ComparisonFunction.Never,
                    BorderColor = SwitchBorder(samplerDescription.Border),
                    MinLOD = samplerDescription.MinLOD,
                    MaxLOD = samplerDescription.MaxLOD,
                    ShaderRegister = sampler.Key,
                    RegisterSpace = 0,
                    ShaderVisibility = ShaderVisibility.All,
                    Flags = SamplerFlags.None
                };
            }

            ResultChecker.ThrowIfUnhandled(_device.D3D12DeviceConfiguration.SerializeVersionedRootSignature(new VersionedRootSignatureDescription(signatureDesc), out Blob result, out Blob error));
            try
            {
                if (error != null && error.BufferSize > 0)
                {
                    throw new RHIException(error.AsString(), -1, device);
                }

                ResultChecker.ThrowIfUnhandled(device.D3D12Device.CreateRootSignature(0, result, out _rootSignature!));
            }
            finally
            {
                result?.Dispose();
                error?.Dispose();
            }

            _stateDescription = new GraphicsPipelineStateDescription
            {
                RootSignature = _rootSignature,
                VertexShader = bytecode.Vertex,
                PixelShader = bytecode.Pixel,
                DomainShader = ReadOnlyMemory<byte>.Empty,
                HullShader = ReadOnlyMemory<byte>.Empty,
                GeometryShader = ReadOnlyMemory<byte>.Empty,
                StreamOutput = null,
                BlendState = new Vortice.Direct3D12.BlendDescription
                {
                    AlphaToCoverageEnable = desc.AlphaToCoverageEnable,
                    IndependentBlendEnable = desc.IndependentBlendEnable,
                },
                SampleMask = uint.MaxValue,
                RasterizerState = new RasterizerDescription
                {
                    FillMode = desc.FillMode switch
                    {
                        FillMode.Solid => Vortice.Direct3D12.FillMode.Solid,
                        FillMode.Wireframe => Vortice.Direct3D12.FillMode.Wireframe,
                        _ => throw new NotImplementedException()
                    },
                    CullMode = desc.CullMode switch
                    {
                        CullMode.None => Vortice.Direct3D12.CullMode.None,
                        CullMode.Back => Vortice.Direct3D12.CullMode.Front,
                        CullMode.Front => Vortice.Direct3D12.CullMode.Back,
                        _ => throw new NotImplementedException()
                    },
                    FrontCounterClockwise = desc.FrontCounterClockwise,
                    DepthBias = desc.DepthBias,
                    DepthBiasClamp = desc.DepthBiasClamp,
                    SlopeScaledDepthBias = desc.SlopeScaledDepthBias,
                    DepthClipEnable = desc.DepthClipEnable,
                    MultisampleEnable = false,
                    AntialiasedLineEnable = false,
                    ForcedSampleCount = 0,
                    ConservativeRaster = desc.ConservativeRaster ? ConservativeRasterizationMode.On : ConservativeRasterizationMode.Off,
                },
                DepthStencilState = new DepthStencilDescription
                {
                    DepthEnable = desc.DepthEnable,
                    DepthWriteMask = desc.DepthWriteMask switch
                    {
                        DepthWriteMask.None => Vortice.Direct3D12.DepthWriteMask.Zero,
                        DepthWriteMask.All => Vortice.Direct3D12.DepthWriteMask.All,
                        _ => throw new NotImplementedException()
                    },
                    DepthFunc = SwitchComparisonFunc(desc.DepthFunc),
                    StencilEnable = desc.StencilEnable,
                    StencilReadMask = desc.StencilReadMask,
                    StencilWriteMask = desc.StencilReadMask,
                    FrontFace = TranslateStencilFace(desc.FrontFace),
                    BackFace = TranslateStencilFace(desc.BackFace),
                },
                InputLayout = new InputLayoutDescription
                {
                    Elements = desc.InputElements.Length == 0 ? Array.Empty<Vortice.Direct3D12.InputElementDescription>() : new Vortice.Direct3D12.InputElementDescription[desc.InputElements.Length]
                },
                IndexBufferStripCutValue = IndexBufferStripCutValue.Disabled,
                PrimitiveTopologyType = desc.PrimitiveTopology switch
                {
                    PrimitiveTopologyType.Triangle => Vortice.Direct3D12.PrimitiveTopologyType.Triangle,
                    PrimitiveTopologyType.Patch => Vortice.Direct3D12.PrimitiveTopologyType.Patch,
                    PrimitiveTopologyType.Point => Vortice.Direct3D12.PrimitiveTopologyType.Point,
                    PrimitiveTopologyType.Line => Vortice.Direct3D12.PrimitiveTopologyType.Line,
                    _ => throw new NotImplementedException()
                },
                RenderTargetFormats = Array.Empty<Format>(),
                DepthStencilFormat = Format.Unknown,
                SampleDescription = SampleDescription.Default,
                NodeMask = 0,
                CachedPSO = new CachedPipelineState { CachedBlob = nint.Zero, CachedBlobSizeInBytes = 0 }, //TODO: consider self implementing?
                Flags = PipelineStateFlags.None,
            };

            Vortice.Direct3D12.BlendDescription modBlend = _stateDescription.BlendState;
            for (int i = 0; i < desc.Blends.Length; i++)
            {
                ref BlendDescription blend = ref desc.Blends[i];
                ref RenderTargetBlendDescription rtBlend = ref modBlend.RenderTarget[i];

                rtBlend.BlendEnable = blend.BlendEnable;
                rtBlend.LogicOpEnable = false;
                rtBlend.SourceBlend = SwitchBlend(blend.SrcBlend);
                rtBlend.DestinationBlend = SwitchBlend(blend.DstBlend);
                rtBlend.BlendOperation = SwitchBlendOp(blend.BlendOp);
                rtBlend.SourceBlendAlpha = SwitchBlend(blend.SrcBlendAlpha);
                rtBlend.DestinationBlendAlpha = SwitchBlend(blend.DstBlendAlpha);
                rtBlend.BlendOperationAlpha = SwitchBlendOp(blend.BlendOpAlpha);
                rtBlend.LogicOp = Vortice.Direct3D12.LogicOp.Noop;
                rtBlend.RenderTargetWriteMask = (ColorWriteEnable)blend.RenderTargetWriteMask;
            }
            _stateDescription.BlendState = modBlend;

            int j = 0;
            for (int i = 0; i < desc.InputElements.Length; i++)
            {
                ref InputElementDescription input = ref desc.InputElements[i];
                if (input.Format == InputElementFormat.Padding)
                    continue;

                DecodeSemantic(input.Semantic, out string name, out uint index);

                _stateDescription.InputLayout.Elements[j++] = new Vortice.Direct3D12.InputElementDescription
                {
                    SemanticName = name,
                    SemanticIndex = index,
                    Format = FormatConverter.Convert(input.Format),
                    Slot = (uint)input.InputSlot,
                    AlignedByteOffset = input.ByteOffset == 0 ? D3D12.AppendAlignedElement : (uint)input.ByteOffset,
                    Classification = input.InputSlotClass switch
                    {
                        InputClassification.Vertex => Vortice.Direct3D12.InputClassification.PerVertexData,
                        InputClassification.Instance => Vortice.Direct3D12.InputClassification.PerInstanceData,
                        _ => throw new NotImplementedException()
                    },
                    InstanceDataStepRate = (uint)input.InstanceDataStepRate
                };
            }

            if (j != _stateDescription.InputLayout.Elements.Length)
            {
                Vortice.Direct3D12.InputElementDescription[] old = _stateDescription.InputLayout.Elements;
                _stateDescription.InputLayout.Elements = new Vortice.Direct3D12.InputElementDescription[j];

                Array.Copy(old, _stateDescription.InputLayout.Elements, j);
            }

            ResultChecker.ThrowIfUnhandled(device.D3D12Device.CreateGraphicsPipelineState(_stateDescription, out ID3D12PipelineState? pipelineState), _device);
            ExceptionUtility.Assert(_pipelineStates.TryAdd(PipelineState.Default, pipelineState!));

            static ComparisonFunction SwitchComparisonFunc(ComparisonFunc value)
            {
                return value switch
                {
                    ComparisonFunc.None => ComparisonFunction.Never,
                    ComparisonFunc.Never => ComparisonFunction.Never,
                    ComparisonFunc.Less => ComparisonFunction.Less,
                    ComparisonFunc.Equal => ComparisonFunction.Equal,
                    ComparisonFunc.LessEqual => ComparisonFunction.LessEqual,
                    ComparisonFunc.Greater => ComparisonFunction.Greater,
                    ComparisonFunc.NotEqual => ComparisonFunction.NotEqual,
                    ComparisonFunc.GreaterEqual => ComparisonFunction.GreaterEqual,
                    ComparisonFunc.Always => ComparisonFunction.Always,
                    _ => throw new NotImplementedException()
                };
            }
            static Vortice.Direct3D12.Blend SwitchBlend(Blend value)
            {
                return value switch
                {
                    Blend.Zero => Vortice.Direct3D12.Blend.Zero,
                    Blend.One => Vortice.Direct3D12.Blend.One,
                    Blend.SourceColor => Vortice.Direct3D12.Blend.SourceColor,
                    Blend.InverseSourceColor => Vortice.Direct3D12.Blend.InverseSourceColor,
                    Blend.SourceAlpha => Vortice.Direct3D12.Blend.SourceAlpha,
                    Blend.InverseSourceAlpha => Vortice.Direct3D12.Blend.InverseSourceAlpha,
                    Blend.DestinationAlpha => Vortice.Direct3D12.Blend.DestinationAlpha,
                    Blend.InverseDestinationAlpha => Vortice.Direct3D12.Blend.InverseDestinationAlpha,
                    Blend.DestinationColor => Vortice.Direct3D12.Blend.DestinationColor,
                    Blend.InverseDestinationColor => Vortice.Direct3D12.Blend.InverseDestinationColor,
                    Blend.SourceAlphaSaturate => Vortice.Direct3D12.Blend.SourceAlphaSaturate,
                    Blend.BlendFactor => Vortice.Direct3D12.Blend.BlendFactor,
                    Blend.InverseBlendFactor => Vortice.Direct3D12.Blend.InverseBlendFactor,
                    Blend.Source1Color => Vortice.Direct3D12.Blend.Source1Color,
                    Blend.InverseSource1Color => Vortice.Direct3D12.Blend.InverseSource1Color,
                    Blend.Source1Alpha => Vortice.Direct3D12.Blend.Source1Alpha,
                    Blend.InverseSource1Alpha => Vortice.Direct3D12.Blend.InverseSource1Alpha,
                    Blend.AlphaFactor => Vortice.Direct3D12.Blend.AlphaFactor,
                    Blend.InverseAlphaFactor => Vortice.Direct3D12.Blend.InverseAlphaFactor,
                    _ => throw new NotImplementedException()
                };
            }
            static BlendOperation SwitchBlendOp(BlendOp value)
            {
                return value switch
                {
                    BlendOp.Add => BlendOperation.Add,
                    BlendOp.Subtract => BlendOperation.Subtract,
                    BlendOp.ReverseSubtract => BlendOperation.RevSubtract,
                    BlendOp.Minimum => BlendOperation.Min,
                    BlendOp.Maximum => BlendOperation.Max,
                    _ => throw new NotImplementedException()
                };
            }
            static DepthStencilOperationDescription TranslateStencilFace(StencilFace face)
            {
                return new DepthStencilOperationDescription
                {
                    StencilFailOp = SwitchStencilOp(face.StencilFailOp),
                    StencilDepthFailOp = SwitchStencilOp(face.StencilDepthFailOp),
                    StencilPassOp = SwitchStencilOp(face.StencilPassOp),
                    StencilFunc = SwitchComparisonFunc(face.StencilFunc),
                };
            }
            static StencilOperation SwitchStencilOp(StencilOp value)
            {
                return value switch
                {
                    StencilOp.Keep => StencilOperation.Keep,
                    StencilOp.Zero => StencilOperation.Zero,
                    StencilOp.Replace => StencilOperation.Replace,
                    StencilOp.IncrementSaturation => StencilOperation.IncrementSaturate,
                    StencilOp.DecrementSaturation => StencilOperation.DecrementSaturate,
                    StencilOp.Invert => StencilOperation.Invert,
                    StencilOp.Increment => StencilOperation.Increment,
                    StencilOp.Decrement => StencilOperation.Decrement,
                    _ => throw new NotImplementedException()
                };
            }
            static void DecodeSemantic(InputElementSemantic semantic, out string name, out uint index)
            {
                InputElementSemantic rounded = (InputElementSemantic)(MathF.Floor((float)semantic / 8.0f) * 8);
                name = rounded.ToString().ToUpper();
                index = semantic - rounded;
            }
            static Filter SwitchFilter(TextureFilter filter)
            {
                return filter switch
                {
                    TextureFilter.Point => Filter.MinMagMipPoint,
                    TextureFilter.MinMagPointMipLinear => Filter.MinMagPointMipLinear,
                    TextureFilter.MinPointMagLinearMipPoint => Filter.MinPointMagLinearMipPoint,
                    TextureFilter.MinPointMagMipLinear => Filter.MinPointMagMipLinear,
                    TextureFilter.MinLinearMagMipPoint => Filter.MinLinearMagMipPoint,
                    TextureFilter.MinLinearMagPointMipLinear => Filter.MinLinearMagPointMipLinear,
                    TextureFilter.MinMagLinearMipPoint => Filter.MinMagLinearMipPoint,
                    TextureFilter.Linear => Filter.MinMagMipLinear,
                    TextureFilter.MinMagAnisotropicMipPoint => Filter.MinMagAnisotropicMipPoint,
                    _ => throw new NotImplementedException()
                };
            }
            static Vortice.Direct3D12.TextureAddressMode SwitchAddressMode(TextureAddressMode addressMode)
            {
                return addressMode switch
                {
                    TextureAddressMode.Repeat => Vortice.Direct3D12.TextureAddressMode.Wrap,
                    TextureAddressMode.Mirror => Vortice.Direct3D12.TextureAddressMode.Mirror,
                    TextureAddressMode.ClampToEdge => Vortice.Direct3D12.TextureAddressMode.Clamp,
                    TextureAddressMode.ClampToBorder => Vortice.Direct3D12.TextureAddressMode.Border,
                    _ => throw new NotImplementedException()
                };
            }
            static StaticBorderColor SwitchBorder(SamplerBorder border)
            {
                return border switch
                {
                    SamplerBorder.TransparentBlack => StaticBorderColor.TransparentBlack,
                    SamplerBorder.OpaqueBlack => StaticBorderColor.OpaqueBlack,
                    SamplerBorder.OpaqueWhite => StaticBorderColor.OpaqueWhite,
                    SamplerBorder.OpaqueBlackUInt => StaticBorderColor.OpaqueBlackUInt,
                    SamplerBorder.OpaqueWhiteUInt => StaticBorderColor.OpaqueWhiteUInt,
                    _ => throw new NotImplementedException()
                };
            }

            static GraphicsPipelineDescription CloneDescription(ref GraphicsPipelineDescription description)
            {
                GraphicsPipelineDescription newDesc = description;
                newDesc.Blends = (BlendDescription[])description.Blends.Clone();
                newDesc.InputElements = (InputElementDescription[])description.InputElements.Clone();
                newDesc.BoundResources = (BoundResourceDescription[])description.BoundResources.Clone();
                newDesc.ImmutableSamplers = (KeyValuePair<uint, ImmutableSamplerDescription>[])description.ImmutableSamplers.Clone();

                return newDesc;
            }
            static GraphicsPipelineBytecode CloneBytecode(ref GraphicsPipelineBytecode bytecode)
            {
                return new GraphicsPipelineBytecode
                {
                    Vertex = bytecode.Vertex.ToArray(),
                    Pixel = bytecode.Pixel.ToArray(),
                };
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    foreach (ID3D12PipelineState? pipelineState in _pipelineStates.Values)
                        pipelineState?.Dispose();
                    _rootSignature?.Dispose();

                    _pipelineStates.Clear();
                });

                _disposedValue = true;
            }
        }

        ~GraphicsPipelineImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void CreateNewPipelineState(ref PipelineState newState, int hashKey, out ID3D12PipelineState? pipelineState)
        {
            if (newState.RTVs.IsEmpty)
            {
                _stateDescription.RenderTargetFormats = Array.Empty<Format>();
            }
            else
            {
                _stateDescription.RenderTargetFormats = _rtvFormatArray;
                for (int i = 0; i < newState.RTVs.Length; i++)
                {
                    RenderTargetFormat rtFormat = newState.RTVs[i];
                    if (rtFormat == RenderTargetFormat.Undefined)
                        _stateDescription.RenderTargetFormats[i] = Format.Unknown;
                    else
                        _stateDescription.RenderTargetFormats[i] = FormatConverter.Convert(rtFormat);
                }
            }

            if (newState.DSV == DepthStencilFormat.Undefined)
                _stateDescription.DepthStencilFormat = Format.Unknown;
            else
                _stateDescription.DepthStencilFormat = FormatConverter.Convert(newState.DSV, false);

            ResultChecker.PrintIfUnhandled(_device.D3D12Device.CreateGraphicsPipelineState(_stateDescription, out pipelineState), _device);
            ExceptionUtility.Assert(_pipelineStates.TryAdd(hashKey, pipelineState));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ID3D12PipelineState? GetPipelineState(ref PipelineState state)
        {
            int hashCode = state.GetHashCode();
            if (!_pipelineStates.TryGetValue(hashCode, out ID3D12PipelineState? pipelineState))
                CreateNewPipelineState(ref state, hashCode, out pipelineState);
            return pipelineState;
        }

        //TODO: implement
        public override string Name { set { } }

        internal bool IsUsingConstantBuffer => _needsConstantBuffer;
        internal ID3D12RootSignature ID3D12RootSignature => _rootSignature;

        public override ref readonly GraphicsPipelineDescription Description => ref _description;
        public override ref readonly GraphicsPipelineBytecode Bytecode => ref _bytecode;
    }

    internal record struct PipelineState
    {
        internal RTVUnion RTVs;
        internal DepthStencilFormat DSV;

        internal static readonly int Default = new PipelineState().GetHashCode();

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
        internal struct RTVUnion
        {
            private ulong _e08;

            public int Length => 8;
            public bool IsEmpty => _e08 == 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Clear() => _e08 = 0;

            internal unsafe RenderTargetFormat this[int index]
            {
#if DEBUG
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ExceptionUtility.Assert(index > -1 && index < 9);
                    return *(RenderTargetFormat*)((byte*)Unsafe.AsPointer(ref _e08) + index);
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    ExceptionUtility.Assert(index > -1 && index < 9);
                    *((byte*)Unsafe.AsPointer(ref _e08) + index) = (byte)value;
                }
#else
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return *(RenderTargetFormat*)((byte*)Unsafe.AsPointer(ref _e08) + index);
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    *((byte*)Unsafe.AsPointer(ref _e08) + index) = (byte)value;
                }
#endif
            }
        }
    }
}
