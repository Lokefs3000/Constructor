using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Structures
{
    public ref struct RasterCommandBuffer
    {
        private readonly RSCommandBuffer _commandBuffer;

        internal RasterCommandBuffer(RSCommandBuffer commandBuffer)
        {
            _commandBuffer = commandBuffer;
        }

        public void SetRenderTarget(int slot, FrameGraphTexture renderTarget) => _commandBuffer.SetRenderTarget(slot, renderTarget);
        public void SetDepthStencil(FrameGraphTexture depthStencil) => _commandBuffer.SetDepthStencil(depthStencil);

        public void ClearRenderTarget(FrameGraphTexture renderTarget, FGRect? rect = null) => _commandBuffer.ClearRenderTarget(renderTarget, null, rect);
        public void ClearDepthStencil(FrameGraphTexture depthStencil, FGClearFlags clearFlags, FGRect? rect = null) => _commandBuffer.ClearDepthStencil(depthStencil, clearFlags, null, null, rect);

        public void ClearRenderTarget(FrameGraphTexture renderTarget, Color color, FGRect? rect = null) => _commandBuffer.ClearRenderTarget(renderTarget, color, rect);
        public void ClearDepthStencil(FrameGraphTexture depthStencil, FGClearFlags clearFlags, float depth = 1.0f, byte stencil = 0xff, FGRect? rect = null) => _commandBuffer.ClearDepthStencil(depthStencil, clearFlags, depth, stencil, rect);

        public void SetViewport(int slot, FGViewport? viewport) => _commandBuffer.SetViewport(slot, viewport);
        public void SetScissor(int slot, FGRect? scissor) => _commandBuffer.SetScissor(slot, scissor);

        public void SetStencilReference(uint stencilRef) => _commandBuffer.SetStencilReference(stencilRef);

        public void SetVertexBuffer(FGSetBufferDesc desc) => _commandBuffer.SetVertexBuffer(desc);
        public void SetIndexBuffer(FGSetBufferDesc desc) => _commandBuffer.SetIndexBuffer(desc);

        public void SetPipeline(ShaderAsset shader) => _commandBuffer.SetPipeline(shader);
        public void SetPipeline(RHIGraphicsPipeline pipeline) => _commandBuffer.SetPipeline(pipeline);

        public void SetProperties(PropertyBlock block) => _commandBuffer.SetProperties(block);
        public void SetProperties(ROPropertyBlock block) => _commandBuffer.SetProperties(block);

        public void SetConstants<T>(T data) where T : unmanaged => _commandBuffer.SetConstants(data);
        public void SetConstants(ReadOnlySpan<uint> data) => _commandBuffer.SetConstants(data);

        public void DrawInstanced(FGDrawInstancedDesc desc) => _commandBuffer.DrawInstanced(desc);
        public void DrawIndexedInstanced(FGDrawIndexedInstancedDesc desc) => _commandBuffer.DrawIndexedInstanced(desc);

        public void Upload<T>(FGBufferUploadDesc desc, ReadOnlySpan<T> data) where T : unmanaged => _commandBuffer.Upload(desc, data);
        public void Upload<T>(FrameGraphBuffer buffer, T data) where T : unmanaged => _commandBuffer.Upload(buffer, data);

        public void Upload<T>(FGTextureUploadDesc desc, Span<T> data) where T : unmanaged => _commandBuffer.Upload(desc, data);

        public void Copy(FGBufferCopyDesc desc) => _commandBuffer.Copy(desc);
        public void Copy(FGTextureCopyDesc desc) => _commandBuffer.Copy(desc);

        public void Read(FGReadBufferDesc desc) => _commandBuffer.Read(desc);
        public void Read(FGReadTextureDesc desc) => _commandBuffer.Read(desc);

        public FGMappedSubresource<T> Map<T>(FGMapBufferDesc desc) where T : unmanaged => _commandBuffer.Map<T>(desc);
        public FGMappedSubresource<T> Map<T>(FrameGraphTexture texture) where T : unmanaged => _commandBuffer.Map<T>(texture);

        internal void PresentOnWindow(Window window) => _commandBuffer.PresentOnWindow(window);

        public CommandEventScope BeginEvent(ReadOnlySpan<byte> name, uint? color = null) => _commandBuffer.BeginEvent(name, color);
        public void MarkEvent(ReadOnlySpan<byte> name, uint? color = null) => _commandBuffer.MarkEvent(name, color);
    }
}
