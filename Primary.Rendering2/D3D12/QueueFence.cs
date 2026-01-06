using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.D3D12_FENCE_FLAGS;

namespace Primary.Rendering2.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class QueueFence : IDisposable
    {
        private ID3D12Fence* _frameFence;
        private ulong _frameFenceValue;
        private ManualResetEventSlim _frameWaitEvent;
        private bool disposedValue;

        internal QueueFence(NRDDevice device)
        {
            ID3D12Fence* ptr = null;

            HRESULT hr = device.Device->CreateFence(0, D3D12_FENCE_FLAG_NONE, UuidOf.Get<ID3D12Fence>(), (void**)&ptr);

            if (hr.FAILED)
            {
                device.RHIDevice.FlushMessageQueue();
                throw new NotImplementedException("Add error message");
            }

            _frameFence = ptr;
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

                if (_frameFence != null)
                    _frameFence->Release();

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
            if (_frameFence->GetCompletedValue() < _frameFenceValue)
            {
                _frameFence->SetEventOnCompletion(_frameFenceValue, HANDLE.NULL);
            }
        }
    }
}
