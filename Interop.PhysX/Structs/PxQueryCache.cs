using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxQueryCache
    {
        public PxShape* shape;
        public PxRigidActor* actor;
        public uint faceIndex;
    }
}
