using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxCapsuleGeometry
    {
        public PxGeometry @base;
        public float radius;
        public float halfHeight;
    }
}
