using Primary.Common;

namespace Primary.RenderLayer
{
    public record struct GfxRenderTextureView : IDisposable
    {
        private RHI.RenderTextureView? _internal;

        public GfxRenderTextureView() => throw new NotSupportedException();
        internal GfxRenderTextureView(RHI.RenderTextureView? renderTextureView) => _internal = renderTextureView;

        public void Dispose() => _internal?.Dispose();

        #region Base



        #endregion

        public nint Handle => NullableUtility.ThrowIfNull(_internal).Handle;

        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public bool IsNull => _internal == null;
        public RHI.RenderTextureView? RHIRenderTextureView => _internal;
        public GfxResource Resource => new GfxResource(_internal);

        public static GfxRenderTextureView Null = new GfxRenderTextureView(null);

        public static explicit operator RHI.RenderTextureView?(GfxRenderTextureView renderTextureView) => renderTextureView._internal;
        public static implicit operator GfxRenderTextureView(RHI.RenderTextureView? renderTextureView) => new GfxRenderTextureView(renderTextureView);

        public static explicit operator GfxResource(GfxRenderTextureView renderTextureView) => new GfxResource(renderTextureView._internal);
    }
}
