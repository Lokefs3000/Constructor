using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI
{
    public unsafe abstract class RHIReadback : IDisposable, IAsNativeObject<RHIReadbackNative>
    {
        protected RHIReadbackDescription _description;

        protected string? _debugName;

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);
        protected abstract void SetDebugName(string? debugName);

        ~RHIReadback()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public abstract bool Read(Span<byte> data);
        public abstract bool Read<T>(Span<T> data) where T : unmanaged;

        public abstract bool IsDataReady { get; }

        public ref readonly RHIReadbackDescription Description => ref _description;

        public string? DebugName
        {
            get => _debugName;
            set
            {
                if (_debugName != value)
                    SetDebugName(value);
                _debugName = value;
            }
        }

        public abstract RHIReadbackNative* GetAsNative();
    }

    public struct RHIReadbackNative
    {
        public RHIReadbackDescription Description;
    }

    public struct RHIReadbackDescription
    {
        public uint Width;

        public RHIReadbackDescription()
        {
            Width = 0;
        }
    }
}
