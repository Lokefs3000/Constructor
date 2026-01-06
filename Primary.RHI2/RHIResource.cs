using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public abstract class RHIResource : IDisposable
    {
        protected string? _debugName;

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);
        protected abstract void SetDebugName(string? debugName);

        ~RHIResource()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public string? DebugName
        {
            get => _debugName;
            set
            {
                if (_debugName != value)
                    SetDebugName(_debugName);
                _debugName = value;
            }
        }

        public abstract RHIResourceType Type { get; }
    }
}
