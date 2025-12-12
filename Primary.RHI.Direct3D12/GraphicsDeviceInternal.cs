using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D12;
using Vortice.DXGI;

using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.RHI.Direct3D12
{
    public struct GraphicsDeviceInternal
    {
        public IDXGIAdapter4 Adapter;
        public ID3D12Device14 Device;

        public Ptr<D3D12MemAlloc.Allocator> Allocator;

        public ID3D12CommandQueue GraphicsQueue;
        public ID3D12CommandQueue ComputeQueue;
        public ID3D12CommandQueue CopyQueue;

        public static unsafe GraphicsDeviceInternal CreateFrom(GraphicsDevice device)
        {
            if (device is not GraphicsDeviceImpl impl)
                throw new ArgumentException(nameof(device));

            return new GraphicsDeviceInternal
            {
                Adapter = impl.DXGIAdapter,
                Device = impl.D3D12Device,
                Allocator = impl.D3D12MAAllocator,
                GraphicsQueue = impl.DirectCommandQueue,
                ComputeQueue = impl.DirectCommandQueue,
                CopyQueue = impl.DirectCommandQueue,
            };
        }
    }
}
