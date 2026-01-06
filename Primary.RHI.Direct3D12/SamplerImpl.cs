using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.RHI.Direct3D12
{
    internal sealed class SamplerImpl : Sampler
    {
        private readonly GraphicsDeviceImpl _device;
        private SamplerDescription _description;

        private GCHandle _handle;
        private nint _handlePtr;

        private bool _disposedValue;

        internal SamplerImpl(GraphicsDeviceImpl device, SamplerDescription description)
        {
            _device = device;
            _description = description;

            _handle = GCHandle.Alloc(this, GCHandleType.Weak);
            _handlePtr = GCHandle.ToIntPtr(_handle);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _device.EnqueueDataFree(() =>
                {
                    _handle.Free();
                    _handlePtr = nint.Zero;
                });

                _disposedValue = true;
            }
        }

        ~SamplerImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override ref readonly SamplerDescription Description => ref _description;
        
        public override nint Handle => _handlePtr;
    }
}
