using System.Numerics;

namespace Primary.RHI
{
    public abstract class GraphicsDevice : IDisposable
    {
        public abstract GraphicsAPI API { get; }
        public abstract string Name { get; }

        public abstract ulong TotalVideoMemory { get; }

        public abstract void Dispose();

        /// <summary>Not thread-safe</summary>
        public abstract void SynchronizeDevice(SynchronizeDeviceTargets targets);

        /// <summary>Not thread-safe</summary>
        public abstract void BeginFrame();
        /// <summary>Not thread-safe</summary>
        public abstract void FinishFrame();

        /// <summary>Thread-safe</summary>
        public abstract void Submit(CommandBuffer commandBuffer);

        /// <summary>Thread-safe</summary>
        public abstract SwapChain CreateSwapChain(Vector2 clientSize, nint windowHandle);
        /// <summary>Thread-safe</summary>
        public abstract Buffer CreateBuffer(BufferDescription description, nint bufferData);
        /// <summary>Thread-safe</summary>
        public abstract Texture CreateTexture(TextureDescription description, Span<nint> textureData);
        /// <summary>Thread-safe</summary>
        public abstract PipelineLibrary CreatePipelineLibrary(Span<byte> initialData);
        /// <summary>Thread-safe</summary>
        public abstract GraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDescription description, GraphicsPipelineBytecode bytecode);
        /// <summary>Thread-safe</summary>
        public abstract CopyCommandBuffer CreateCopyCommandBuffer();
        /// <summary>Thread-safe</summary>
        public abstract GraphicsCommandBuffer CreateGraphicsCommandBuffer();
        /// <summary>Thread-safe</summary>
        public abstract RenderTarget CreateRenderTarget(RenderTargetDescription description);
        /// <summary>Thread-safe</summary>
        public abstract Fence CreateFence(ulong initialValue = 0);

        /// <summary>Thread-safe</summary>
        public abstract bool IsSupported(RenderTargetFormat format);
        /// <summary>Thread-safe</summary>
        public abstract bool IsSupported(DepthStencilFormat format);
        /// <summary>Thread-safe</summary>
        public abstract bool IsSupported(TextureFormat format, TextureDimension dimension);

        /// <summary>Not thread-safe</summary>
        public abstract void InstallTracker(IObjectTracker tracker);
        /// <summary>Not thread-safe</summary>
        public abstract void UninstallTracker(IObjectTracker tracker);

        //HACK: remove when r2 is integrated fully
        public static GraphicsDevice? Instance = null;
    }

    [Flags]
    public enum SynchronizeDeviceTargets : byte
    {
        None = 0,
        Graphics = 1 << 0,
        Compute = 1 << 1,
        Copy = 1 << 2,

        All = Graphics | Compute | Copy
    }
}
