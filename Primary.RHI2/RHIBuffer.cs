using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public unsafe abstract class RHIBuffer : IDisposable, AsNativeObject<RHIBufferNative>
    {
        protected RHIBufferDescription _description;
        protected string? _debugName;

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);
        protected abstract void SetDebugName(string? debugName);

        ~RHIBuffer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ref readonly RHIBufferDescription Description => ref _description;
        
        public string? DebugName { get => _debugName; set { _debugName = value; SetDebugName(_debugName); } }

        public abstract RHIBufferNative* GetAsNative();
    }

    public struct RHIBufferNative
    {
        public RHIBufferDescription Description;
    }

    public struct RHIBufferDescription
    {
        public uint Width;
        public int Stride;

        public RHIResourceUsage Usage;
        public RHIBufferMode Mode;

        public int FirstElement;
        public int ElementCount;

        public RHIBufferDescription()
        {
            Width = 0;
            Stride = 0;

            Usage = RHIResourceUsage.None;
            Mode = RHIBufferMode.Default;

            FirstElement = 0;
            ElementCount = -1;
        }

        public RHIBufferDescription(in RHIBufferDescription other)
        {
            Width = other.Width;
            Stride = other.Stride;

            Usage = other.Usage;
            Mode = other.Mode;

            FirstElement = other.FirstElement;
            ElementCount = other.ElementCount;
        }
    }
}
