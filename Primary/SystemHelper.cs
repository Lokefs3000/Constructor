using System.Runtime.Intrinsics;

namespace Primary
{
    internal static class SystemHelper
    {
        internal static void EnsureFeaturesPresent()
        {
            if (!Vector128.IsHardwareAccelerated)
                EngLog.Core.Warning("No 128-bit SIMD hardware available! Performance will suffer..");
            if (!Vector256.IsHardwareAccelerated)
                EngLog.Core.Warning("No 256-bit SIMD hardware available! Performance will suffer..");
        }
    }
}
