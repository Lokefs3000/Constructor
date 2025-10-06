using System.Numerics;

namespace Primary.RHI
{
    public abstract class GraphicsDevice : IDisposable
    {
        public abstract GraphicsAPI API { get; }
        public abstract string Name { get; }

        public abstract ulong TotalVideoMemory { get; }

        public abstract void Dispose();

        public abstract void SynchronizeDevice(SynchronizeDeviceTargets targets);

        public abstract void BeginFrame();
        public abstract void FinishFrame();

        public abstract void Submit(CommandBuffer commandBuffer);

        public abstract SwapChain CreateSwapChain(Vector2 clientSize, nint windowHandle);
        public abstract Buffer CreateBuffer(BufferDescription description, nint bufferData);
        public abstract Texture CreateTexture(TextureDescription description, Span<nint> textureData);
        public abstract PipelineLibrary CreatePipelineLibrary(Span<byte> initialData);
        public abstract GraphicsPipeline CreateGraphicsPipeline(GraphicsPipelineDescription description, GraphicsPipelineBytecode bytecode);
        public abstract CopyCommandBuffer CreateCopyCommandBuffer();
        public abstract GraphicsCommandBuffer CreateGraphicsCommandBuffer();
        public abstract RenderTarget CreateRenderTarget(RenderTargetDescription description);
        public abstract Fence CreateFence(ulong initialValue = 0);

        public abstract bool IsSupported(RenderTargetFormat format);
        public abstract bool IsSupported(DepthStencilFormat format);
        public abstract bool IsSupported(TextureFormat format, TextureDimension dimension);

        public abstract void InstallTracker(IObjectTracker tracker);
        public abstract void UninstallTracker(IObjectTracker tracker);
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
