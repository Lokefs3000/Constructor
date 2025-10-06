using Primary.RHI.Direct3D12.Utility;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Primary.RHI.Direct3D12.Memory
{
    internal unsafe sealed class GPURingBuffer : RingBuffer, IDisposable
    {
        private readonly GraphicsDeviceImpl _device;

        private nint _cpuVirtualAddresss;
        private ulong _gpuVirtualAddress;
        private ID3D12Resource _buffer;

        private bool _disposedValue;

        internal GPURingBuffer(ulong maxSize, GraphicsDeviceImpl device, bool allowCpuAccess) : base(maxSize)
        {
            GraphicsDeviceImpl.Logger.Debug("Allocating memory for new GPU ring buffer with size: {a}mb", maxSize / (1024.0 * 1024.0));

            _device = device;

            //TODO: consider using GpuUpload instead of just Upload
            //just remember to now allocate *too* much using that heap as it is limited in size

            HeapProperties props = new HeapProperties
            {
                Type = allowCpuAccess ? HeapType.Upload : HeapType.Default,
                CPUPageProperty = CpuPageProperty.Unknown,
                MemoryPoolPreference = MemoryPool.Unknown,
                CreationNodeMask = 0,
                VisibleNodeMask = 0,
            };

            ResourceDescription desc = new ResourceDescription
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = maxSize,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.Unknown,
                SampleDescription = SampleDescription.Default,
                Layout = TextureLayout.RowMajor,
                Flags = allowCpuAccess ? ResourceFlags.None : ResourceFlags.AllowUnorderedAccess,
            };

            ResourceStates defaultUsage = allowCpuAccess ? ResourceStates.GenericRead : ResourceStates.UnorderedAccess;
            ResultChecker.ThrowIfUnhandled(device.D3D12Device.CreateCommittedResource(props, HeapFlags.None, desc, defaultUsage, null, out _buffer!), device);

            _buffer.Name = $"GPU ring buffer (cpu:{(allowCpuAccess ? 1 : 0)})";

            _gpuVirtualAddress = _buffer.GPUVirtualAddress;

            if (allowCpuAccess)
            {
                void* ptr = null;
                ResultChecker.ThrowIfUnhandled(_buffer.Map(0, &ptr), device);
                _cpuVirtualAddresss = (nint)ptr;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    if (_cpuVirtualAddresss != nint.Zero)
                        _buffer.Unmap(0);
                    _buffer?.Dispose();
                });

                _disposedValue = true;
            }
        }

        ~GPURingBuffer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public new DynamicAllocation Allocate(ulong sizeInBytes)
        {
            ulong offset = base.Allocate(sizeInBytes);
            if (offset != RingBuffer.InvalidOffset)
                return new DynamicAllocation(_buffer, offset, sizeInBytes, (_cpuVirtualAddresss == nint.Zero) ? _cpuVirtualAddresss : _cpuVirtualAddresss + (nint)offset, _gpuVirtualAddress + offset);
            else
                return new DynamicAllocation();
        }
    }

    internal readonly record struct DynamicAllocation(ID3D12Resource? Buffer, ulong Offset, ulong Size, nint CpuAddress, ulong GpuAddress);
}
