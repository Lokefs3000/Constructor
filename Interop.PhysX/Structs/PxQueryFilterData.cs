using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxQueryFilterData
    {
        public PxFilterData data;
        public PxQueryFlag flags;
    }
}
