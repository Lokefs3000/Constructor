using Primary.Common;
using Primary.Rendering2.NRD;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Recording;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D12;
using Vortice.DXGI;

using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.Rendering2.D3D12
{
    internal unsafe sealed class NRDDevice : INativeRenderDispatcher
    {
        private readonly IDXGIAdapter4 _adapter;
        private readonly ID3D12Device14 _device;

        private readonly Ptr<D3D12MemAlloc.Allocator> _allocator;

        private readonly ID3D12CommandQueue _graphicsQueue;
        private readonly ID3D12CommandQueue _computeQueue;
        private readonly ID3D12CommandQueue _copyQueue;

        private Queue<ID3D12CommandAllocator> _allocatorQueue1;
        private Queue<ID3D12CommandAllocator> _allocatorQueue2;

        private bool _drawCycle;

        internal NRDDevice(GraphicsDevice device)
        {
            GraphicsDeviceInternal @internal = GraphicsDeviceInternal.CreateFrom(device);

            _adapter = @internal.Adapter;
            _device = @internal.Device;

            _allocator = @internal.Allocator;

            _graphicsQueue = @internal.GraphicsQueue;
            _computeQueue = @internal.ComputeQueue;
            _copyQueue = @internal.CopyQueue;
        }

        public void Dispatch(FrameGraphTimeline timeline, FrameGraphRecorder recorder)
        {

        }
    }
}
