using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Primary.RHI.Direct3D12
{
    internal unsafe sealed class CompositionSurface : IDisposable
    {
        private readonly CompositionDevice _compositionDevice;

        private IDCompositionTarget* _target;
        private IDCompositionVisual2* _visual;

        private bool _disposedValue;

        internal CompositionSurface(CompositionDevice compositionDevice, void* windowHandle, void* swapChainHandle)
        {
            _compositionDevice = compositionDevice;

            fixed (IDCompositionTarget** targetPtr = &_target)
            {
                HRESULT hr = ((IDCompositionDevice*)compositionDevice.Device)->CreateTargetForHwnd((HWND)windowHandle, false, targetPtr);
                if (hr.FAILED)
                {
                    _target = null;

                    throw new Exception("Failed to create target for window composition") { HResult = hr.Value };
                }
            }

            fixed (IDCompositionVisual2** visualPtr = &_visual)
            {
                HRESULT hr = compositionDevice.Device->CreateVisual(visualPtr);
                if (hr.FAILED)
                {
                    _target->Release();

                    _target = null;
                    _visual = null;

                    throw new Exception("Failed to create visual for window composition") { HResult = hr.Value };
                }
            }

            {
                HRESULT hr = _visual->SetContent((IUnknown*)swapChainHandle);
                if (hr.FAILED)
                {
                    _target->Release();
                    _visual->Release();

                    _target = null;
                    _visual = null;

                    throw new Exception("Failed to set composition visual to swap chain") { HResult = hr.Value };
                }
            }

            {
                HRESULT hr = _target->SetRoot((IDCompositionVisual*)_visual);
                if (hr.FAILED)
                {
                    _target->Release();
                    _visual->Release();

                    _target = null;
                    _visual = null;

                    throw new Exception("Failed to set composition target to swap chain visual") { HResult = hr.Value };
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_visual != null)
                    _visual->Release();
                if (_target != null)
                    _target->Release();

                _visual = null;
                _target = null;

                _compositionDevice.DecrementSurfaceCount();

                _disposedValue = true;
            }
        }

        ~CompositionSurface()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
