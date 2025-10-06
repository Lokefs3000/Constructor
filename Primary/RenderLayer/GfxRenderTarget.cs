using Primary.Common;

namespace Primary.RenderLayer
{
    public record struct GfxRenderTarget : IDisposable
    {
        private RHI.RenderTarget? _internal;

        public GfxRenderTarget() => throw new NotSupportedException();
        internal GfxRenderTarget(RHI.RenderTarget? renderTarget) => _internal = renderTarget;

        public void Dispose() => _internal?.Dispose();

        #region Base



        #endregion

        public ref readonly RHI.RenderTargetDescription Description => ref NullableUtility.ThrowIfNull(_internal).Description;
        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public GfxRenderTextureView ColorTexture => NullableUtility.ThrowIfNull(_internal).ColorTexture ?? GfxRenderTextureView.Null;
        public GfxRenderTextureView DepthTexture => NullableUtility.ThrowIfNull(_internal).DepthTexture ?? GfxRenderTextureView.Null;

        public bool IsNull => _internal == null;
        public RHI.RenderTarget? RHIRenderTarget => _internal;

        public static GfxRenderTarget Null = new GfxRenderTarget(null);

        public static explicit operator RHI.RenderTarget?(GfxRenderTarget renderTarget) => renderTarget._internal;
        public static implicit operator GfxRenderTarget(RHI.RenderTarget? renderTarget) => new GfxRenderTarget(renderTarget);

        public static explicit operator GfxResource(GfxRenderTarget renderTarget) => new GfxResource(renderTarget._internal);
    }
}
