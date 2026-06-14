using Primary.Memory.Native;
using System.Runtime.CompilerServices;

namespace Primary.RHI
{
    public unsafe abstract class RHIDevice : IDisposable, IAsNativeObject<RHIDeviceNative>
    {
        protected static readonly WeakReference s_instance = new WeakReference(null);

        protected bool _disposedValue;

        protected abstract void Dispose(bool disposing);

        ~RHIDevice()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            s_instance.Target = null;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not thread-safe</summary>
        public abstract void HandlePendingUpdates();
        /// <summary>Not thread-safe</summary>
        public abstract void IncrementFrame();

        /// <summary>Thread-safe</summary>
        public abstract RHIBuffer? CreateBuffer(in RHIBufferDescription description, ArrayPtr<byte> rawData, [CallerMemberName] string? debugName = "");
        /// <summary>Thread-safe</summary>
        public abstract RHITexture? CreateTexture(in RHITextureDescription description, Span<ArrayPtr<byte>> planeSlices, [CallerMemberName] string? debugName = "");
        /// <summary>Thread-safe</summary>
        public abstract RHISampler? CreateSampler(in RHISamplerDescription description, [CallerMemberName] string? debugName = "");
        /// <summary>Thread-safe</summary>
        public abstract RHISwapChain? CreateSwapChain(in RHISwapChainDescription description, [CallerMemberName] string? debugName = "");
        /// <summary>Thread-safe</summary>
        public abstract RHIGraphicsPipeline? CreateGraphicsPipeline(in RHIGraphicsPipelineDescription description, in RHIGraphicsPipelineBytecode bytecode, [CallerMemberName] string? debugName = "");
        /// <summary>Thread-safe</summary>
        public abstract RHIComputePipeline? CreateComputePipeline(in RHIComputePipelineDescription description, in RHIComputePipelineBytecode bytecode, [CallerMemberName] string? debugName = "");

        /// <summary>Thread-safe</summary>
        public abstract void FlushPendingMessages();

        /// <summary>Not thread-safe</summary>
        public abstract RHIUsedMemoryInfo QueryUsedMemory();

        public abstract RHIDeviceNative* GetAsNative();

        public abstract RHIDeviceAPI DeviceAPI { get; }

        public static int CalculateMaxMipLevels(int width, int height, int depth) => (int)(Math.Log2(Math.Max(Math.Max(width, height), depth)) + 1);

        public static RHIDevice? Instance => Unsafe.As<RHIDevice>(s_instance.Target);
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

    public struct RHIVideoBudgetInfo
    {

    }

    public readonly record struct RHIUsedMemoryInfo(RHIUsedMemoryBudget Local, RHIUsedMemoryBudget NonLocal);

    public readonly record struct RHIUsedMemoryBudget(int BlockCount, int AllocationCount, long BlockBytes, long AllocationBytes, long TotalBudget, long TotalUsage);
}
