using System.Numerics;

namespace Primary.RHI
{
    public abstract class GraphicsCommandBuffer : CommandBuffer
    {
        public abstract void ClearRenderTarget(RenderTarget rt, Vector4 color);
        public abstract void ClearDepthStencil(RenderTarget rt, ClearFlags clear, float depth = 1.0f, byte stencil = 0xff);

        public abstract void SetRenderTargets(Span<RenderTarget> renderTargets, bool setFirstToDepth = false);
        public abstract void SetDepthStencil(RenderTarget? renderTarget);

        public abstract void SetViewports(Span<Viewport> viewports);
        public abstract void SetScissorRects(Span<ScissorRect> rects);

        public abstract void SetStencilReference(uint stencilRef);

        public abstract void SetVertexBuffers(int startSlot, Span<Buffer> buffers, Span<uint> strides);
        public abstract void SetIndexBuffer(Buffer? buffer);

        public abstract void SetPipeline(GraphicsPipeline pipeline);

        public abstract void SetResources(Span<ResourceLocation> resources);
        public abstract void SetConstants(Span<uint> constants);

        public abstract void DrawIndexedInstanced(in DrawIndexedInstancedArgs args);
        public abstract void DrawInstanced(in DrawInstancedArgs args);
    }

    [Flags]
    public enum ClearFlags : byte
    {
        None = 0,

        Depth = 1 << 0,
        Stencil = 1 << 1,

        Both = Depth | Stencil
    }

    public record struct DrawIndexedInstancedArgs
    {
        public uint IndexCountPerInstance;
        public uint InstanceCount;
        public uint StartIndexLocation;
        public int BaseVertexLocation;
        public uint StartInstanceLocation;

        public DrawIndexedInstancedArgs(uint indexCount, uint startIndexLocation = 0, int baseVertexLocation = 0, uint instanceCount = 1, uint startInstanceLocation = 0)
        {
            IndexCountPerInstance = indexCount;
            InstanceCount = instanceCount;
            StartIndexLocation = startIndexLocation;
            BaseVertexLocation = baseVertexLocation;
            StartInstanceLocation = startInstanceLocation;
        }
    }

    public record struct DrawInstancedArgs
    {
        public uint VertexCountPerInstance;
        public uint InstanceCount;
        public uint StartVertexLocation;
        public uint StartInstanceLocation;

        public DrawInstancedArgs(uint vertexCount, uint startVertexLocation = 0, uint instanceCount = 1, uint startInstanceLocation = 0)
        {
            VertexCountPerInstance = vertexCount;
            InstanceCount = instanceCount;
            StartVertexLocation = startVertexLocation;
            StartInstanceLocation = startInstanceLocation;
        }
    }

    public record struct ResourceLocation(ushort ConstantsOffset, Resource? Resource, Descriptor? Descriptor, uint DescriptorOffset);
    public record struct ScissorRect(int Left, int Top, int Right, int Bottom);
    public record struct Viewport(float TopLeftX, float TopLeftY, float Width, float Height, float MinDepth = 0.0f, float MaxDepth = 1.0f);
}
