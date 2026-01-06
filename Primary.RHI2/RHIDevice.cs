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

        public abstract RHIBuffer CreateBuffer(in RHIBufferDescription description, string? debugName = null);

        public abstract RHIDeviceNative* GetAsNative();
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
