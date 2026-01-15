using CommunityToolkit.HighPerformance;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    public unsafe static class ResourceHelper
    {
        public static bool SetResourceName(ID3D12Resource2* resource, string? newName)
        {
            if (newName != null)
            {
                if (newName.Length > 1023)
                {
                    char* buffer = (char*)NativeMemory.Alloc((nuint)(newName.Length + 1), sizeof(char));
                    Debug.Assert(buffer != null);

                    NativeMemory.Copy(Unsafe.AsPointer(ref newName.DangerousGetReference()), buffer, (nuint)(newName.Length + newName.Length));
                    buffer[newName.Length] = '\0';

                    bool ret = resource->SetName(buffer).SUCCEEDED;

                    NativeMemory.Free(buffer);
                    return ret;
                }
                else
                {
                    char* buffer = stackalloc char[newName.Length + 1];
                    Debug.Assert(buffer != null);

                    NativeMemory.Copy(Unsafe.AsPointer(ref newName.DangerousGetReference()), buffer, (nuint)(newName.Length + newName.Length));
                    buffer[newName.Length] = '\0';

                    return resource->SetName(buffer).SUCCEEDED;
                }
            }
            else
            {
                char n = '\0';
                return resource->SetName(&n).SUCCEEDED;
            }
        }

        public static D3D12_FILTER EncodeBasicFilter(RHIFilterType min, RHIFilterType mag, RHIFilterType mip, RHIReductionType reduction)
        {
            return (D3D12_FILTER)(
                ((((int)min) & D3D12.D3D12_FILTER_TYPE_MASK) << D3D12.D3D12_MIN_FILTER_SHIFT) |
                ((((int)mag) & D3D12.D3D12_FILTER_TYPE_MASK) << D3D12.D3D12_MAG_FILTER_SHIFT) |
                ((((int)mip) & D3D12.D3D12_FILTER_TYPE_MASK) << D3D12.D3D12_MIP_FILTER_SHIFT) |
                ((((int)reduction) & D3D12.D3D12_FILTER_REDUCTION_TYPE_MASK) << D3D12.D3D12_FILTER_REDUCTION_TYPE_SHIFT));
        }

        public static D3D12_FILTER EncodeAnisotropicFilter(RHIReductionType reduction)
        {
            return (D3D12_FILTER)(
                D3D12.D3D12_ANISOTROPIC_FILTERING_BIT |
                (int)EncodeBasicFilter(RHIFilterType.Linear, RHIFilterType.Linear, RHIFilterType.Linear, reduction));
        }

        public static uint EncodeShader4ComponentMapping(uint src0, uint src1, uint src2, uint src3)
        {
            return ((src0) & D3D12.D3D12_SHADER_COMPONENT_MAPPING_MASK) |
                   (((src1) & D3D12.D3D12_SHADER_COMPONENT_MAPPING_MASK) << D3D12.D3D12_SHADER_COMPONENT_MAPPING_SHIFT) |
                   (((src2) & D3D12.D3D12_SHADER_COMPONENT_MAPPING_MASK) << (D3D12.D3D12_SHADER_COMPONENT_MAPPING_SHIFT * 2)) |
                   (((src3) & D3D12.D3D12_SHADER_COMPONENT_MAPPING_MASK) << (D3D12.D3D12_SHADER_COMPONENT_MAPPING_SHIFT * 3)) |
                   D3D12.D3D12_SHADER_COMPONENT_MAPPING_ALWAYS_SET_BIT_AVOIDING_ZEROMEM_MISTAKES;
        }
    }
}
