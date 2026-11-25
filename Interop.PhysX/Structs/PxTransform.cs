using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxTransform
    {
        public PxQuat q;
        public PxVec3 p;
    }
}
