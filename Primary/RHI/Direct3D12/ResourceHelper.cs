using CommunityToolkit.HighPerformance;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe static class ResourceHelper
    {
        public static bool SetResourceName(ref ID3D12Resource2 resource, string? newName) => SetResourceName(ref Unsafe.As<ID3D12Resource2, ID3D12Object>(ref resource), newName);
        public static bool SetResourceName(ref ID3D12RootSignature rootSignature, string? newName) => SetResourceName(ref Unsafe.As<ID3D12RootSignature, ID3D12Object>(ref rootSignature), newName);
        public static bool SetResourceName(ref ID3D12PipelineState pipelineState, string? newName) => SetResourceName(ref Unsafe.As<ID3D12PipelineState, ID3D12Object>(ref pipelineState), newName);

        public static bool SetResourceName(ref ID3D12Object resource, string? newName)
        {
            if (newName != null)
            {
                if (newName.Length > 1023)
                {
                    char* buffer = (char*)NativeMemory.Alloc((nuint)(newName.Length + 1), sizeof(char));
                    Debug.Assert(buffer != null);

                    fixed (char* ptr = newName)
                    {
                        NativeMemory.Copy(ptr, buffer, (nuint)(newName.Length + newName.Length));
                    }
                    buffer[newName.Length] = '\0';

                    bool ret = new HResult(resource.SetName(buffer)).IsSuccess;

                    NativeMemory.Free(buffer);
                    return ret;
                }
                else
                {
                    char* buffer = stackalloc char[newName.Length + 1];
                    Debug.Assert(buffer != null);

                    fixed (char* ptr = newName)
                    {
                        NativeMemory.Copy(ptr, buffer, (nuint)(newName.Length + newName.Length));
                    }
                    buffer[newName.Length] = '\0';

                    return new HResult(resource.SetName(buffer)).IsSuccess;
                }
            }
            else
            {
                char n = '\0';
                return new HResult(resource.SetName(&n)).IsSuccess;
            }
        }

        public static Filter EncodeBasicFilter(RHIFilterType min, RHIFilterType mag, RHIFilterType mip, RHIReductionType reduction)
        {
            return (Filter)(
                ((((int)min) & D3D12.FilterTypeMask) << D3D12.MinFilterShift) |
                ((((int)mag) & D3D12.FilterTypeMask) << D3D12.MagFilterShift) |
                ((((int)mip) & D3D12.FilterTypeMask) << D3D12.MipFilterShift) |
                ((((int)reduction) & D3D12.FilterReductionTypeMask) << D3D12.FilterReductionTypeShift));
        }

        public static Filter EncodeAnisotropicFilter(RHIReductionType reduction)
        {
            return (Filter)(
                D3D12.AnisotropicFilteringBit |
                (int)EncodeBasicFilter(RHIFilterType.Linear, RHIFilterType.Linear, RHIFilterType.Linear, reduction));
        }

        public static uint EncodeShader4ComponentMapping(uint src0, uint src1, uint src2, uint src3)
        {
            return ((src0) & D3D12.ShaderComponentMappingMask) |
                   (((src1) & D3D12.ShaderComponentMappingMask) << D3D12.ShaderComponentMappingShift) |
                   (((src2) & D3D12.ShaderComponentMappingMask) << (D3D12.ShaderComponentMappingShift * 2)) |
                   (((src3) & D3D12.ShaderComponentMappingMask) << (D3D12.ShaderComponentMappingShift * 3)) |
                   (1 << (D3D12.ShaderComponentMappingShift * 4));
        }

        public static (int arrayIndex, int mipLevel) DecodeSubresource(int subresource, int mipLevels) => (subresource / mipLevels, subresource % mipLevels);

        public static int Align(int value, int alignment) => value + (-value & (alignment - 1));
        public static long Align(long value, long alignment) => value + (-value & (alignment - 1));
    }
}
