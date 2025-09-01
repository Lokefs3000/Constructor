using Primary.Common.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RHI.Direct3D12.Allocators
{
    internal unsafe sealed class LinearAllocator : IAllocator
    {
        private uint _capacity;
        private uint _position;

        internal LinearAllocator(int capacity)
        {
            _capacity = (uint)capacity;
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }

        internal void Reset()
        {
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAllocator.Allocation Allocate(int size)
        {
            if (_position + size > _capacity)
                return new Allocation(InvalidOffset);

            uint offset = _position;
            _position += (uint)size;

            return new Allocation(offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free(ref IAllocator.Allocation allocation)
        {
            
        }

        public const uint InvalidOffset = uint.MaxValue;

        private readonly record struct Allocation : IAllocator.Allocation
        {
            public readonly uint InternalValue;

            internal Allocation(uint value)
            {
                InternalValue = value;
            }

            public uint Value => InternalValue;

            public bool IsNull => InternalValue == InvalidOffset;
            public bool IsNotNull => InternalValue != InvalidOffset;
        }
    }
}
