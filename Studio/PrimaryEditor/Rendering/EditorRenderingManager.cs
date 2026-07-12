using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Rendering
{
    public sealed class EditorRenderingManager : IDisposable
    {
        private Gizmos _gizmos;
        private ScreenGizmos _screenGizmos;

        private bool _disposedValue;

        internal EditorRenderingManager()
        {
            _gizmos = new Gizmos();
            _screenGizmos = new ScreenGizmos();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _gizmos.Dispose();
                    _screenGizmos.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        
        internal void PrepareForFrame()
        {
            _gizmos.ClearDrawData();
            _screenGizmos.ClearDrawData();
        }
    }
}
