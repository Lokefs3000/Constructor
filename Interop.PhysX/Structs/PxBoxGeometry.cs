using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxBoxGeometry
    {
        public PxGeometry @base;
        public PxVec3 halfExtents;
    }
}
