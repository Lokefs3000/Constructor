using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Reflection
{
    public sealed class ReflectionManager : IDisposable
    {
        private AssemblyTypeLoader _typeLoader;

        private bool _disposedValue;

        internal ReflectionManager()
        {
            _typeLoader = new AssemblyTypeLoader();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _typeLoader.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ReflectCurrentData()
        {
            _typeLoader.ScanLoadedAssemblies();
        }

        public AssemblyTypeLoader TypeLoader => _typeLoader;
    }
}
