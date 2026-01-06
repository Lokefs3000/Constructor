using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.DirectX;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe static class ResourceHelper
    {
        internal static bool SetResourceName(ID3D12Resource2* resource, string? newName)
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
                return resource->SetName(null).SUCCEEDED;
            }
        }
    }
}
