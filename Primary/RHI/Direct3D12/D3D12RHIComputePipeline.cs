using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe sealed class D3D12RHIComputePipeline : RHIComputePipeline
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<ID3D12RootSignature> _rootSignature;
        private ComPtr<ID3D12PipelineState> _pipelineState;

        private D3D12RHIComputePipelineNative* _nativeRep;

        internal D3D12RHIComputePipeline(D3D12RHIDevice device, RHIComputePipelineDescription description, RHIComputePipelineBytecode bytecode)
        {
            _device = device;

            _description = new RHIComputePipelineDescription(description);
            _bytecode = new RHIComputePipelineBytecode(bytecode);

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

            {
                using MemoryHandle handle = _bytecode.Compute.Pin();

                ComputePipelineStateDesc desc = new ComputePipelineStateDesc
                {
                    PRootSignature = (ID3D12RootSignature*)Unsafe.AsPointer(ref _rootSignature.Get()),
                    CS = new ShaderBytecode(handle.Pointer, (nuint)_bytecode.Compute.Length),
                    NodeMask = 0,
                    CachedPSO = default,
                    Flags = PipelineStateFlags.None
                };

                HResult hr = _device.Device.CreateComputePipelineState(&desc, out _pipelineState);
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create D3D12 pipeline state", hr.Value);
                }
            }

            {
                _nativeRep = (D3D12RHIComputePipelineNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIComputePipelineNative>());
                _nativeRep->Base = new RHIComputePipelineNative
                {

                };
                _nativeRep->RootSignature = (ID3D12RootSignature*)Unsafe.AsPointer(ref _rootSignature.Get());
                _nativeRep->PipelineState = (ID3D12PipelineState*)Unsafe.AsPointer(ref _pipelineState.Get());
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

                    _pipelineState.Dispose();
                    _rootSignature.Dispose();

                    _device.ResourceTracker.Untrack(this);
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (!Unsafe.IsNullRef(in _rootSignature.Get()))
            {
                ResourceHelper.SetResourceName(ref _rootSignature.Get(), debugName == null ? null : $"{debugName}-RootSig");
            }

            if (!Unsafe.IsNullRef(in _pipelineState.Get()))
            {
                ResourceHelper.SetResourceName(ref _pipelineState.Get(), debugName);
            }
        }

        public override string ToString()
        {
            return $"RHIComputePipeline{{{_debugName}}}";
        }

        public override unsafe RHIComputePipelineNative* GetAsNative() => (RHIComputePipelineNative*)_nativeRep;

        public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
        public ComPtr<ID3D12PipelineState> PipelineState => _pipelineState;
    }

    public unsafe struct D3D12RHIComputePipelineNative
    {
        public RHIComputePipelineNative Base;

        public ID3D12RootSignature* RootSignature;
        public ID3D12PipelineState* PipelineState;

        public static implicit operator RHIComputePipelineNative(D3D12RHIComputePipelineNative native) => native.Base;
    }
}
