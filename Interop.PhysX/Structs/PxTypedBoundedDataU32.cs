using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxTypedBoundedDataU32
    {
        public uint* data;
        public uint stride;
        public uint count;
    }
}
