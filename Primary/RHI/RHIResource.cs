using Primary.Rendering;

namespace Primary.RHI
{
    public abstract class RHIResource : IDisposable
    {
        protected RHIBarrierState _barrierState;
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

        public abstract unsafe RHIResourceNative* GetBaseAsNative();

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

        internal ref RHIBarrierState BarrierState => ref _barrierState;

        public abstract RHIResourceType Type { get; }
    }

    public struct RHIResourceNative
    {

    }

    public readonly record struct RHIAlignmentInfo(int Alignment, int SizeInBytes);
    public readonly record struct RHIBarrierState(ResourceBarrierSync Sync, ResourceBarrierAccess Access, BarrierQueueSpec QueueSpec);
}
