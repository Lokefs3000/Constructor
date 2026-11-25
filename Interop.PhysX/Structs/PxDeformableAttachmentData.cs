using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxDeformableAttachmentData
    {
        public PxActor* actor0;
        public PxActor* actor1;
        public PxDeformableAttachmentTargetType type0;
        public PxDeformableAttachmentTargetType type1;
        public PxTypedBoundedDataU32 indices0;
        public PxTypedBoundedDataU32 indices1;
        public PxTypedBoundedDataVec4 coords0;
        public PxTypedBoundedDataVec4 coords1;
        public PxTransform pose0;
        public PxTransform pose1;
    }
}
