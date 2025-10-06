using Primary.Common;

namespace Primary.RHI.Direct3D12.Memory
{
    internal unsafe sealed class DynamicUploadHeap : IDisposable
    {
        private readonly GraphicsDeviceImpl _device;

        private bool _isCpuAccessible;
        private List<GPURingBuffer> _ringBuffers;

        private bool _disposedValue;

        internal DynamicUploadHeap(GraphicsDeviceImpl device, bool isCpuAccesible, ulong initialSize)
        {
            _device = device;

            _isCpuAccessible = isCpuAccesible;
            _ringBuffers = [new GPURingBuffer(initialSize, device, isCpuAccesible)];
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (GPURingBuffer ringBuffer in _ringBuffers)
                        ringBuffer.Dispose();
                    _ringBuffers.Clear();
                }

                _disposedValue = true;
            }
        }

        ~DynamicUploadHeap()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public DynamicAllocation Allocate(ulong sizeInBytes, ulong alignment = 0)
        {
            ulong alignmnetMask = alignment - 1;
            ExceptionUtility.Assert((alignmnetMask & alignment) == 0);

            ulong alignedSize = Math.Max((sizeInBytes + alignmnetMask) & ~alignment, sizeInBytes);
            DynamicAllocation allocation = _ringBuffers[_ringBuffers.Count - 1].Allocate(alignedSize);
            if (allocation.Buffer == null)
            {
                ulong newMaxSize = _ringBuffers[_ringBuffers.Count - 1].MaxSize << 1;
                while (newMaxSize < sizeInBytes) newMaxSize <<= 1; //faster then mul2

                GPURingBuffer ringBuffer = new GPURingBuffer(newMaxSize, _device, _isCpuAccessible);
                _ringBuffers.Add(ringBuffer);

                allocation = ringBuffer.Allocate(alignedSize);
            }

            return allocation;
        }

        public void FinishFrame(ulong fenceValue, ulong lastCompletedFenceValue)
        {
            int numBuffsToDelete = 0;
            ulong sizeReleased = 0;

            for (int i = 0; i < _ringBuffers.Count; i++)
            {
                GPURingBuffer ringBuffer = _ringBuffers[i];
                ringBuffer.FinishCurrentFrame(fenceValue);
                ringBuffer.ReleaseCompletedFrames(lastCompletedFenceValue);

                if (numBuffsToDelete == i && i < _ringBuffers.Count - 1 && ringBuffer.IsEmpty)
                {
                    numBuffsToDelete++;
                    sizeReleased += ringBuffer.MaxSize;
                }
            }

            if (numBuffsToDelete > 0)
            {
                GraphicsDeviceImpl.Logger.Debug("Releasing #{num} gpu ring buffers using a total of {mem}mb of data from internal list..", numBuffsToDelete, sizeReleased / (1024.0 * 1024.0));

                for (int i = 0; i < numBuffsToDelete; i++)
                {
                    _ringBuffers[i].Dispose();
                }

                _ringBuffers.RemoveRange(0, numBuffsToDelete);
            }
        }
    }
}
