using Primary.RHI.Direct3D12.Allocators;
using Primary.RHI.Direct3D12.Utility;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Descriptors
{
    internal readonly record struct DescriptorHeapAllocation
    {
        public readonly IAllocator.Allocation Allocation;

        public readonly CpuDescriptorHandle CpuHandle;
        public readonly GpuDescriptorHandle GpuHandle;

        public readonly uint DescriptorCount;
        public readonly ushort DescriptorSize;

        public readonly IDescriptorHeap Heap;
        public readonly ushort ManagerId;

        public readonly ushort HeapOffset;

        public DescriptorHeapAllocation(IAllocator.Allocation allocation, CpuDescriptorHandle cpu, GpuDescriptorHandle gpu, uint descriptorCount, ushort descriptorSize, IDescriptorHeap heap, ushort managerId, ushort heapOffset)
        {
            Allocation = allocation;
            CpuHandle = cpu;
            GpuHandle = gpu;
            DescriptorCount = descriptorCount;
            DescriptorSize = descriptorSize;
            Heap = heap;
            ManagerId = managerId;
            HeapOffset = heapOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CpuDescriptorHandle GetCpuHandle(int offset = 0)
        {
            if (offset == 0)
                return CpuHandle;
            return CpuHandle.NewOffseted(offset * DescriptorSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GpuDescriptorHandle GetGpuHandle(int offset = 0)
        {
            if (offset == 0)
                return GpuHandle;
            return GpuHandle.NewOffseted(offset * DescriptorSize);
        }

        public bool IsNull => Heap == null;

        public static readonly DescriptorHeapAllocation Null = new DescriptorHeapAllocation();
    }
}
