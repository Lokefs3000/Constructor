using System;

namespace Interop.PhysX
{
    public readonly record struct PxShapeGPUIndex(uint Value)
    {
        public static implicit operator PxShapeGPUIndex(uint value) => new(value);
        public static implicit operator uint(PxShapeGPUIndex value) => value.Value;
    }
}
