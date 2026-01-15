using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
using static TerraFX.Interop.DirectX.D3D12_LOGIC_OP;
using static TerraFX.Interop.DirectX.D3D12_PIPELINE_STATE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_ROOT_DESCRIPTOR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_ROOT_PARAMETER_TYPE;
using static TerraFX.Interop.DirectX.D3D12_ROOT_SIGNATURE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_SAMPLER_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_SHADER_VISIBILITY;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
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

                D3D12_STATIC_SAMPLER_DESC1[] samplers = description.ImmutableSamplers.Length == 0 ?
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

            {
                using MemoryHandle handle = _bytecode.Compute.Pin();

                D3D12_COMPUTE_PIPELINE_STATE_DESC desc = new D3D12_COMPUTE_PIPELINE_STATE_DESC
                {
                    pRootSignature = _rootSignature.Get(),
                    CS = new D3D12_SHADER_BYTECODE(handle.Pointer, (nuint)_bytecode.Compute.Length),
                    NodeMask = 0,
                    CachedPSO = default,
                    Flags = D3D12_PIPELINE_STATE_FLAG_NONE
                };

                HRESULT hr = _device.Device.Get()->CreateComputePipelineState(&desc, UuidOf.Get<ID3D12PipelineState>(), (void**)_pipelineState.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create D3D12 pipeline state: {hr}");
                }
            }

            {
                _nativeRep = (D3D12RHIComputePipelineNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIComputePipelineNative>());
                _nativeRep->Base = new RHIComputePipelineNative
                {

                };
                _nativeRep->RootSignature = (ComPtr<ID3D12RootSignature>*)Unsafe.AsPointer(ref _rootSignature);
                _nativeRep->PipelineState = (ComPtr<ID3D12PipelineState>*)Unsafe.AsPointer(ref _pipelineState);
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

                    _pipelineState.Reset();
                    _rootSignature.Reset();
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (_rootSignature.Get() != null)
            {
                ResourceHelper.SetResourceName((ID3D12Resource2*)_rootSignature.Get(), debugName == null ? null : $"{debugName}-RootSig");
            }

            if (_pipelineState.Get() != null)
            {
                ResourceHelper.SetResourceName((ID3D12Resource2*)_pipelineState.Get(), debugName);
            }
        }

        public override unsafe RHIComputePipelineNative* GetAsNative() => (RHIComputePipelineNative*)_nativeRep;

        public ComPtr<ID3D12RootSignature> RootSignature => _rootSignature;
        public ComPtr<ID3D12PipelineState> PipelineState => _pipelineState;
    }

    public unsafe struct D3D12RHIComputePipelineNative
    {
        public RHIComputePipelineNative Base;

        public ComPtr<ID3D12RootSignature>* RootSignature;
        public ComPtr<ID3D12PipelineState>* PipelineState;

        public static implicit operator RHIComputePipelineNative(D3D12RHIComputePipelineNative native) => native.Base;
    }
}
