using Primary.RHI.Direct3D12.Allocators;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Descriptors
{
    internal sealed class CpuDescriptorHeap : IDescriptorHeap, IDisposable
    {
        private readonly GraphicsDeviceImpl _device;

        private List<DescriptorHeapAllocationManager> _heaps;

        private DescriptorHeapType _heapType;
        private uint _heapSize;

        private bool _disposedValue;

        internal CpuDescriptorHeap(GraphicsDeviceImpl device, DescriptorHeapType heapType, uint heapSize)
        {
            _device = device;

            _heaps = new List<DescriptorHeapAllocationManager>
            {
                new DescriptorHeapAllocationManager(device, this, 0, new CpuAllocator((int)heapSize), heapType, heapSize, false)
            };

            _heapType = heapType;
            _heapSize = heapSize;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (DescriptorHeapAllocationManager heap in _heaps)
                {
                    heap.Dispose();
                }

                _heaps.Clear();

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal DescriptorHeapAllocation Rent(int size)
        {
            for (int i = 0; i < _heaps.Count; i++)
            {
                DescriptorHeapAllocation alloc = _heaps[i].Allocate(size);
                if (!alloc.IsNull)
                    return alloc;
            }

            DescriptorHeapAllocationManager allocationManager = new DescriptorHeapAllocationManager(_device, this, (ushort)_heaps.Count, new CpuAllocator((int)_heapSize), _heapType, _heapSize, false);
            _heaps.Add(allocationManager);

            return allocationManager.Allocate(size);
        }

        internal void Return(DescriptorHeapAllocation allocation)
        {
            _heaps[allocation.ManagerId].Free(ref allocation);
        }

        internal void ReleaseStaleAllocations()
        {
            for (int i = 0; i < _heaps.Count; i++)
            {
                _heaps[i].ReleaseStaleAllocations();
            }
        }

        internal CpuDescriptorHandle NullDescriptor => _heaps[0].NullDescriptor;
    }
}
