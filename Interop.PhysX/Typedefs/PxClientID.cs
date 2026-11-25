using System;

namespace Interop.PhysX
{
    public readonly record struct PxClientID(sbyte Value)
    {
        public static implicit operator PxClientID(sbyte value) => new(value);
        public static implicit operator sbyte(PxClientID value) => value.Value;
    }
}
