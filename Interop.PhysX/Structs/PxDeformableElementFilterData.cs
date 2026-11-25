using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxDeformableElementFilterData
    {
        public PxActor* actor0;
        public PxActor* actor1;
        public PxTypedBoundedDataU32 groupElementCounts0;
        public PxTypedBoundedDataU32 groupElementCounts1;
        public PxTypedBoundedDataU32 groupElementIndices0;
        public PxTypedBoundedDataU32 groupElementIndices1;
    }
}
