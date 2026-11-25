using System;

namespace Interop.PhysX
{
    public enum PxRigidDynamicLockFlag : sbyte
    {
        eLOCK_LINEAR_X = 1,
        eLOCK_LINEAR_Y = 2,
        eLOCK_LINEAR_Z = 4,
        eLOCK_ANGULAR_X = 8,
        eLOCK_ANGULAR_Y = 16,
        eLOCK_ANGULAR_Z = 32
    }
}
