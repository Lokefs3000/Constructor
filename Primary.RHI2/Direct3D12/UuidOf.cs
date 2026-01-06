using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    internal static unsafe class UuidOf
    {
        internal static Guid* Get<T>() where T : unmanaged, INativeGuid => Windows.__uuidof<T>();
    }
}
