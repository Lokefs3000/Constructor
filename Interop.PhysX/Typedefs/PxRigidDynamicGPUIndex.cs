using System;

namespace Interop.PhysX
{
    public readonly record struct PxRigidDynamicGPUIndex(uint Value)
    {
        public static implicit operator PxRigidDynamicGPUIndex(uint value) => new(value);
        public static implicit operator uint(PxRigidDynamicGPUIndex value) => value.Value;
    }
}
