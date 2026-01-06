namespace Primary.RHI2
{
    public unsafe abstract class RHIDevice : IDisposable, AsNativeObject<RHIDeviceNative>
    {
        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);

        ~RHIDevice()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not thread-safe</summary>
        public abstract void HandlePendingUpdates();

        /// <summary>Thread-safe</summary>
        public abstract RHIBuffer? CreateBuffer(in RHIBufferDescription description, string? debugName = null);
        /// <summary>Thread-safe</summary>
        public abstract RHITexture? CreateTexture(in RHITextureDescription description, string? debugName = null);
        /// <summary>Thread-safe</summary>
        public abstract RHISampler? CreateSampler(in RHISamplerDescription description, string? debugName = null);
        /// <summary>Thread-safe</summary>
        public abstract RHISwapChain? CreateSwapChain(in RHISwapChainDescription description, string? debugName = null);
        /// <summary>Thread-safe</summary>
        public abstract RHIGraphicsPipeline? CreateGraphicsPipeline(in RHIGraphicsPipelineDescription description, in RHIGraphicsPipelineBytecode bytecode, string? debugName = null);

        /// <summary>Thread-safe</summary>
        public abstract void FlushPendingMessages();

        public abstract RHIDeviceNative* GetAsNative();

        public abstract RHIDeviceAPI DeviceAPI { get; }

        public static int CalculateMaxMipLevels(int width, int height, int depth) => (int)(Math.Log2(Math.Max(Math.Max(width, height), depth)) + 1);
    }

    public struct RHIDeviceNative
    {

    }

    public struct RHIDeviceDescription
    {
        public bool EnableValidation;

        public RHIDeviceDescription()
        {
            EnableValidation = false;
        }

        public RHIDeviceDescription(in RHIDeviceDescription other)
        {
            EnableValidation = other.EnableValidation;
        }
    }
}
