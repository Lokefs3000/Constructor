using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.RHI;
using System.Runtime.CompilerServices;

namespace Primary.RenderLayer
{
    public record struct GfxGraphicsCommandBuffer : IDisposable
    {
        private RHI.GraphicsCommandBuffer? _internal;

        public GfxGraphicsCommandBuffer() => throw new NotSupportedException();
        internal GfxGraphicsCommandBuffer(RHI.GraphicsCommandBuffer? commandBuffer) => _internal = commandBuffer;

        public void Dispose() => _internal?.Dispose();

        #region Base

        public bool Begin() => NullableUtility.ThrowIfNull(_internal).Begin();
        public void End() => NullableUtility.ThrowIfNull(_internal).End();

        public nint Map(GfxBuffer buffer, RHI.MapIntent intent, ulong writeSize = 0, ulong writeOffset = 0) => NullableUtility.ThrowIfNull(_internal).Map(NullableUtility.ThrowIfNull(buffer.RHIBuffer), intent, writeSize, writeOffset);
        public nint Map(GfxTexture texture, RHI.MapIntent intent, RHI.TextureLocation location, uint subresource = 0, uint rowPitch = 0) => NullableUtility.ThrowIfNull(_internal).Map(NullableUtility.ThrowIfNull(texture.RHITexture), intent, location, subresource, rowPitch);
        public void Unmap(GfxResource resource) => NullableUtility.ThrowIfNull(_internal).Unmap(NullableUtility.ThrowIfNull(resource.RHIResource));

        public bool CopyBufferRegion(GfxBuffer src, uint srcOffset, GfxBuffer dst, uint dstOffset, uint size) => NullableUtility.ThrowIfNull(_internal).CopyBufferRegion(NullableUtility.ThrowIfNull(src.RHIBuffer), srcOffset, NullableUtility.ThrowIfNull(dst.RHIBuffer), dstOffset, size);
        public bool CopyTextureRegion(GfxResource src, RHI.TextureLocation srcLoc, uint srcSubRes, GfxResource dst, RHI.TextureLocation dstLoc, uint dstSubRes) => NullableUtility.ThrowIfNull(_internal).CopyTextureRegion(NullableUtility.ThrowIfNull(src.RHIResource), srcLoc, srcSubRes, NullableUtility.ThrowIfNull(dst.RHIResource), dstLoc, dstSubRes);

        public void BeginEvent(Color32 color, ReadOnlySpan<char> name) => NullableUtility.ThrowIfNull(_internal).BeginEvent(color, name);
        public void EndEvent() => NullableUtility.ThrowIfNull(_internal).EndEvent();
        public void SetMarker(Color32 color, ReadOnlySpan<char> name) => NullableUtility.ThrowIfNull(_internal).SetMarker(color, name);

        public void ClearRenderTarget(GfxRenderTarget rt, Color color) => NullableUtility.ThrowIfNull(_internal).ClearRenderTarget(NullableUtility.ThrowIfNull(rt.RHIRenderTarget), color.AsVector4());
        public void ClearRenderTarget(GfxBackBuffer backBuffer, Color color) => NullableUtility.ThrowIfNull(_internal).ClearRenderTarget(NullableUtility.ThrowIfNull(backBuffer.RHIRenderTarget), color.AsVector4());
        public void ClearDepthStencil(GfxRenderTarget rt, RHI.ClearFlags clear, float depth = 1.0f, byte stencil = 0xff) => NullableUtility.ThrowIfNull(_internal).ClearDepthStencil(NullableUtility.ThrowIfNull(rt.RHIRenderTarget), clear, depth, stencil);

        public unsafe void SetRenderTargets(Span<GfxRenderTarget> renderTargets, bool setFirstToDepth = false) => NullableUtility.ThrowIfNull(_internal).SetRenderTargets(new Span<RHI.RenderTarget>(Unsafe.AsPointer(ref renderTargets.DangerousGetReference()), renderTargets.Length), setFirstToDepth);
        public void SetDepthStencil(GfxRenderTarget renderTarget) => NullableUtility.ThrowIfNull(_internal).SetDepthStencil(renderTarget.RHIRenderTarget);

        public void SetViewports(Span<RHI.Viewport> viewports) => NullableUtility.ThrowIfNull(_internal).SetViewports(viewports);
        public void SetScissorRects(Span<RHI.ScissorRect> scissorRects) => NullableUtility.ThrowIfNull(_internal).SetScissorRects(scissorRects);

        public void SetStencilReference(uint stencilRef) => NullableUtility.ThrowIfNull(_internal).SetStencilReference(stencilRef);

        public unsafe void SetVertexBuffers(int startSlot, Span<GfxBuffer> vertexBuffers) => NullableUtility.ThrowIfNull(_internal).SetVertexBuffers(startSlot, new Span<RHI.Buffer>(Unsafe.AsPointer(ref vertexBuffers.DangerousGetReference()), vertexBuffers.Length), Span<uint>.Empty);
        public void SetIndexBuffer(GfxBuffer indexBuffer) => NullableUtility.ThrowIfNull(_internal).SetIndexBuffer(indexBuffer.RHIBuffer);

        public void SetPipeline(GfxGraphicsPipeline pipeline) => NullableUtility.ThrowIfNull(_internal).SetPipeline(NullableUtility.ThrowIfNull(pipeline.RHIGraphicsPipeline));

        public void SetResources(Span<ResourceLocation> resources) => NullableUtility.ThrowIfNull(_internal).SetResources(resources);
        public void SetConstants(Span<uint> constants) => NullableUtility.ThrowIfNull(_internal).SetConstants(constants);

        public void DrawIndexedInstanced(in DrawIndexedInstancedArgs args) => NullableUtility.ThrowIfNull(_internal).DrawIndexedInstanced(args);
        public void DrawInstanced(in DrawInstancedArgs args) => NullableUtility.ThrowIfNull(_internal).DrawInstanced(args);

        #endregion

        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public bool IsOpen => NullableUtility.ThrowIfNull(_internal).IsOpen;
        public bool IsReady => NullableUtility.ThrowIfNull(_internal).IsReady;

        public RHI.CommandBufferType Type => NullableUtility.ThrowIfNull(_internal).Type;

        public bool IsNull => _internal == null;
        public RHI.GraphicsCommandBuffer? RHIGraphicsCommandBuffer => _internal;
        public GfxCommandBuffer CommandBuffer => new GfxCommandBuffer(_internal);

        public static GfxGraphicsCommandBuffer Null = new GfxGraphicsCommandBuffer(null);

        public static explicit operator RHI.GraphicsCommandBuffer?(GfxGraphicsCommandBuffer commandBuffer) => commandBuffer._internal;
        public static implicit operator GfxGraphicsCommandBuffer(RHI.GraphicsCommandBuffer? commandBuffer) => new GfxGraphicsCommandBuffer(commandBuffer);

        public static explicit operator GfxCommandBuffer(GfxGraphicsCommandBuffer commandBuffer) => new GfxCommandBuffer(commandBuffer._internal);
    }
}
