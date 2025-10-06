using Primary.RHI.Direct3D12.Allocators;
using Primary.RHI.Direct3D12.Utility;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Descriptors
{
    internal class GpuDescriptorHeap : IDescriptorHeap, IDisposable
    {
        private LinearAllocator _allocator;

        private uint _descriptorCount;
        private ushort _descriptorSize;

        private ID3D12DescriptorHeap _heap;

        private CpuDescriptorHandle _cpuStart;
        private GpuDescriptorHandle _gpuStart;

        private bool _disposedValue;

        internal GpuDescriptorHeap(GraphicsDeviceImpl device, uint size, DescriptorHeapType heapType)
        {
            _allocator = new LinearAllocator((int)size);

            _descriptorCount = size;
            _descriptorSize = (ushort)device.D3D12Device.GetDescriptorHandleIncrementSize(heapType);

            ResultChecker.ThrowIfUnhandled(device.D3D12Device.CreateDescriptorHeap(new DescriptorHeapDescription(heapType, size, DescriptorHeapFlags.ShaderVisible), out _heap!), device);

            _cpuStart = _heap.GetCPUDescriptorHandleForHeapStart();
            _gpuStart = _heap.GetGPUDescriptorHandleForHeapStart();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _heap.Dispose();
                _allocator.Dispose();

                _disposedValue = true;
            }
        }

        ~GpuDescriptorHeap()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal DescriptorHeapAllocation Allocate(int size)
        {
            IAllocator.Allocation alloc = _allocator.Allocate(size);
            if (alloc.IsNull)
                return DescriptorHeapAllocation.Null;

            CpuDescriptorHandle cpu = _cpuStart.NewOffseted((int)(alloc.Value * _descriptorSize));
            GpuDescriptorHandle gpu = _gpuStart.NewOffseted((int)(alloc.Value * _descriptorSize));

            return new DescriptorHeapAllocation(alloc, cpu, gpu, (uint)size, _descriptorSize, this, 0, (ushort)alloc.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Free(ref DescriptorHeapAllocation allocation) { }

        internal void ReleaseStaleAllocations()
        {
            _allocator.Reset();
        }

        internal ID3D12DescriptorHeap Heap => _heap;
    }
}
