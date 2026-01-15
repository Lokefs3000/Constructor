using System.Numerics;

namespace Primary.RHI2
{
    public unsafe abstract class RHISwapChain : IDisposable, IAsNativeObject<RHISwapChainNative>
    {
        protected RHISwapChainDescription _description;

        protected string? _debugName;

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);
        protected abstract void SetDebugName(string? debugName);

        ~RHISwapChain()
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

        public abstract void Present();
        public abstract void Resize(Vector2 newSize);

        public ref RHISwapChainDescription Description => ref _description;

        public abstract RHISwapChainNative* GetAsNative();
    }

    public struct RHISwapChainNative
    {
        public RHISwapChainDescription Description;
    }

    public struct RHISwapChainDescription
    {
        public nint WindowHandle;
        public Vector2 WindowSize;

        public RHIFormat BackBufferFormat;
        public int BackBufferCount;

        public RHISwapChainDescription()
        {
            WindowHandle = nint.Zero;
            WindowSize = Vector2.Zero;

            BackBufferFormat = RHIFormat.Unknown;
            BackBufferCount = 0;
        }

        public RHISwapChainDescription(RHISwapChainDescription other)
        {
            WindowHandle = other.WindowHandle;
            WindowSize = other.WindowSize;

            BackBufferFormat = other.BackBufferFormat;
            BackBufferCount = other.BackBufferCount;
        }
    }
}
