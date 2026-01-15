using System.Runtime.Versioning;
using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal static unsafe class UuidOf
    {
        internal static Guid* Get<T>() where T : unmanaged, INativeGuid => Windows.__uuidof<T>();
    }
}
