using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.RHI
{
    public abstract class Resource : IDisposable
    {
        public abstract nint Handle { get; }

        public abstract void Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Resource? FromIntPtr(nint ptr) => GCHandle.FromIntPtr(ptr).Target as Resource;
    }

    public enum MemoryUsage : byte
    {
        Immutable = 0,
        Default,
        Dynamic,
        Staging,
        Readback
    }

    [Flags]
    public enum CPUAccessFlags : byte
    {
        None = 0,
        Write = 1 << 0,
        Read = 1 << 1,
    }
}
