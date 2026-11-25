using System;

namespace Interop.PhysX
{
    public readonly record struct PxDominanceGroup(sbyte Value)
    {
        public static implicit operator PxDominanceGroup(sbyte value) => new(value);
        public static implicit operator sbyte(PxDominanceGroup value) => value.Value;
    }
}
