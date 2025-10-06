using Primary.Rendering.Pass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering
{
    public sealed class RenderPassInterface : IDisposable
    {
        private RPBufferManager _bufferManager;

        private bool _disposedValue;

        internal RenderPassInterface()
        {
            _bufferManager = new RPBufferManager();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _bufferManager.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void PreFramePrepare()
        {
            _bufferManager.AnalyzeFrameBuffers();
        }

        public PassBuffer<T> Rent<T>() where T : unmanaged => _bufferManager.Rent<T>();
    }
}
