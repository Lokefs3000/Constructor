using System;

namespace Interop.PhysX
{
    public readonly record struct PxType(ushort Value)
    {
        public static implicit operator PxType(ushort value) => new(value);
        public static implicit operator ushort(PxType value) => value.Value;
    }
}
