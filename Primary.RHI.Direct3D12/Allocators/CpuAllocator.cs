using Primary.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.RHI.Direct3D12.Allocators
{
    internal unsafe class CpuAllocator : IAllocator
    {
        private Stack<uint> _slab;
        private uint _slabCapacity;

        internal CpuAllocator(int descriptorCount)
        {
            _slab = new Stack<uint>(descriptorCount);
            _slabCapacity = (uint)descriptorCount;

            for (uint i = 0; i < descriptorCount; i++)
            {
                _slab.Push(i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAllocator.Allocation Allocate(int size)
        {
            ExceptionUtility.Assert(size == 1);
            return new Allocation
            {
                Value = _slab.Pop(),
                Size = (byte)size
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free(ref IAllocator.Allocation allocation)
        {
            Debug.Assert(allocation.Value < _slabCapacity);
            Debug.Assert(_slab.Count + 1 < _slabCapacity);

            _slab.Push(allocation.Value);
        }

        private record struct Allocation : IAllocator.Allocation
        {
            public uint Value;
            public byte Size;

            public bool IsNull => Size == 0;
            public bool IsNotNull => Size > 0;

            uint IAllocator.Allocation.Value => Value;
        }

        private record struct FreeAllocation
        {

        }
    }
}
