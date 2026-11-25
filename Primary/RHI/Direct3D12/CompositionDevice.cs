using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class CompositionDevice : IDisposable
    {
        private IDCompositionDevice4* _device;
        private int _surfaceCount;

        private bool _disposedValue;

        internal CompositionDevice(IDXGIDevice4* dxgiDevice)
        {
            fixed(IDCompositionDevice4** devicePtr = &_device)
            {
                HRESULT hr = DirectX.DCompositionCreateDevice3((IUnknown*)dxgiDevice, UuidOf.Get<IDCompositionDevice4>(), (void**)devicePtr);
                if (hr.FAILED)
                {
                    _device = null;
                    throw new Exception($"Failed to create composition device") { HResult = hr.Value };
                }
            }

            _surfaceCount = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_device != null)
                    _device->Release();
                _device = null;

                _disposedValue = true;
            }
        }

        ~CompositionDevice()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void CommitSurfaces()
        {
            if (_surfaceCount > 0)
            {
                // _device->WaitForCommitCompletion();
                _device->Commit();
            }
        }

        internal CompositionSurface CreateSurface(void* windowHandle, void* swapChainHandle)
        {
            CompositionSurface surface = new CompositionSurface(this, windowHandle, swapChainHandle);
            ++_surfaceCount;

            return surface;
        }

        internal void DecrementSurfaceCount() => --_surfaceCount;

        internal IDCompositionDevice4* Device => _device;
    }
}
