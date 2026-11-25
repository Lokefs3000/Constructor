using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxBounds3
    {
        public PxVec3 minimum;
        public PxVec3 maximum;
    }
}
