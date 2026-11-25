using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxTypedBoundedDataVec4
    {
        public PxVec4* data;
        public uint stride;
        public uint count;
    }
}
