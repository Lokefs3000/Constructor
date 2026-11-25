using System.Runtime.Versioning;
using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal static unsafe class UuidOf
    {
        internal static Guid* Get<T>() where T : unmanaged, INativeGuid => Windows.__uuidof<T>();
    }
}
