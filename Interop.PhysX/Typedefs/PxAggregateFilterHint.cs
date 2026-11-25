using System;

namespace Interop.PhysX
{
    public readonly record struct PxAggregateFilterHint(uint Value)
    {
        public static implicit operator PxAggregateFilterHint(uint value) => new(value);
        public static implicit operator uint(PxAggregateFilterHint value) => value.Value;
    }
}
