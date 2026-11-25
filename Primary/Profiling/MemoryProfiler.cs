using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Profiling
{
    public sealed class MemoryProfiler : IDisposable
    {
        private readonly ProfilingManager _manager;

        private bool _disposedValue;

        internal MemoryProfiler(ProfilingManager manager)
        {
            _manager = manager;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
