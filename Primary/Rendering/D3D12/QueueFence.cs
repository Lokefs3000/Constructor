using System.Runtime.Versioning;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class QueueFence : IDisposable
    {
        private ComPtr<ID3D12Fence> _frameFence;
        private ulong _frameFenceValue;
        private ManualResetEventSlim _frameWaitEvent;
        private bool disposedValue;

        internal QueueFence(NRDDevice device)
        {
            HResult hr = device.Device->CreateFence(0, FenceFlags.None, out _frameFence);
            if (hr.IsFailure)
            {
                device.RHIDevice.FlushPendingMessages();
                throw new NotImplementedException("Add error message");
            }

            _frameFenceValue = 0;
            _frameWaitEvent = new ManualResetEventSlim(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Wait();

                if (disposing)
                {
                    _frameWaitEvent.Dispose();
                }

                _frameFence.Dispose();

                disposedValue = true;
            }
        }

        ~QueueFence()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Signal(ID3D12CommandQueue* queue)
        {
            queue->Signal(_frameFence, ++_frameFenceValue);
        }

        internal void Wait()
        {
            if (_frameFence.GetCompletedValue() < _frameFenceValue)
            {
                _frameFence.SetEventOnCompletion(_frameFenceValue, null);
            }
        }
    }
}
