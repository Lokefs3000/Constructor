using Primary.Common;

namespace Primary.RenderLayer
{
    public record struct GfxCopyCommandBuffer : IDisposable
    {
        private RHI.CopyCommandBuffer? _internal;

        public GfxCopyCommandBuffer() => throw new NotSupportedException();
        internal GfxCopyCommandBuffer(RHI.CopyCommandBuffer? commandBuffer) => _internal = commandBuffer;

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

        #endregion

        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public bool IsOpen => NullableUtility.ThrowIfNull(_internal).IsOpen;
        public bool IsReady => NullableUtility.ThrowIfNull(_internal).IsReady;

        public RHI.CommandBufferType Type => NullableUtility.ThrowIfNull(_internal).Type;

        public bool IsNull => _internal == null;
        public RHI.CopyCommandBuffer? RHICopyCommandBuffer => _internal;
        public GfxCommandBuffer CommandBuffer => new GfxCommandBuffer(_internal);

        public static GfxCopyCommandBuffer Null = new GfxCopyCommandBuffer(null);

        public static explicit operator RHI.CopyCommandBuffer?(GfxCopyCommandBuffer commandBuffer) => commandBuffer._internal;
        public static implicit operator GfxCopyCommandBuffer(RHI.CopyCommandBuffer? commandBuffer) => new GfxCopyCommandBuffer(commandBuffer);

        public static explicit operator GfxCommandBuffer(GfxCopyCommandBuffer commandBuffer) => new GfxCommandBuffer(commandBuffer._internal);
    }
}
