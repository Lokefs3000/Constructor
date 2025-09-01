using Primary.RHI.Direct3D12.Allocators;
using Primary.RHI.Direct3D12.Utility;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Descriptors
{
    internal class DescriptorHeapAllocationManager : IDisposable
    {
        private IAllocator _allocator;

        private uint _descriptorCount;
        private ushort _descriptorSize;

        private ID3D12DescriptorHeap _heap;
        private bool _isShaderVisible;

        private CpuDescriptorHandle _cpuStart;
        private GpuDescriptorHandle _gpuStart;

        private IDescriptorHeap _parent;
        private ushort _managerId;

        private Queue<PendingFree> _pending;

        private DescriptorHeapAllocation _nullAllocation;

        private bool _disposedValue;

        internal DescriptorHeapAllocationManager(GraphicsDeviceImpl device, IDescriptorHeap heap, ushort managerId, IAllocator allocator, DescriptorHeapType heapType, uint descriptorCount, bool isShaderVisible)
        {
            _allocator = allocator;

            _descriptorCount = descriptorCount;
            _descriptorSize = (ushort)device.D3D12Device.GetDescriptorHandleIncrementSize(heapType);

            ResultChecker.ThrowIfUnhandled(device.D3D12Device.CreateDescriptorHeap(new DescriptorHeapDescription(heapType, descriptorCount, isShaderVisible ? DescriptorHeapFlags.ShaderVisible : DescriptorHeapFlags.None), out _heap!), device);
            _isShaderVisible = isShaderVisible;

            _cpuStart = _heap.GetCPUDescriptorHandleForHeapStart();
            _gpuStart = isShaderVisible ? _heap.GetGPUDescriptorHandleForHeapStart() : GpuDescriptorHandle.Default;

            _parent = heap;
            _managerId = managerId;

            _pending = new Queue<PendingFree>();

            _nullAllocation = Allocate(1);

            switch (heapType)
            {
                case DescriptorHeapType.RenderTargetView:
                    {
                        device.D3D12Device.CreateRenderTargetView(null, new RenderTargetViewDescription
                        {
                            ViewDimension = RenderTargetViewDimension.Texture2D,
                            Format = Vortice.DXGI.Format.R8G8B8A8_UNorm,
                            Texture2D = new Texture2DRenderTargetView
                            {
                                MipSlice = 0,
                                PlaneSlice = 0
                            }
                        }, _nullAllocation.GetCpuHandle());
                        break;
                    }
                case DescriptorHeapType.DepthStencilView:
                    {
                        device.D3D12Device.CreateDepthStencilView(null, new DepthStencilViewDescription
                        {
                            ViewDimension = DepthStencilViewDimension.Texture2D,
                            Format = Vortice.DXGI.Format.D24_UNorm_S8_UInt,
                            Texture2D = new Texture2DDepthStencilView
                            {
                                MipSlice = 0,
                            }
                        }, _nullAllocation.GetCpuHandle());
                        break;
                    }
            }
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

        ~DescriptorHeapAllocationManager()
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
            GpuDescriptorHandle gpu = _gpuStart.Ptr > 0 ? _gpuStart.NewOffseted((int)(alloc.Value * _descriptorSize)) : GpuDescriptorHandle.Default;

            return new DescriptorHeapAllocation(alloc, cpu, gpu, (uint)size, _descriptorSize, _parent, _managerId, (ushort)alloc.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Free(ref DescriptorHeapAllocation allocation)
        {
            if (allocation.IsNull)
                return;

            _pending.Enqueue(new PendingFree
            {
                Allocation = allocation.Allocation
            });
        }

        internal void ReleaseStaleAllocations()
        {
            while (_pending.TryDequeue(out PendingFree free))
            {
                _allocator.Free(ref free.Allocation);
            }
        }

        internal CpuDescriptorHandle NullDescriptor => _nullAllocation.CpuHandle;

        private record struct PendingFree
        {
            public IAllocator.Allocation Allocation;
        }
    }
}
