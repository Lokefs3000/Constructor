using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RenderLayer
{
    public record struct GfxBackBuffer : IDisposable
    {
        private RHI.RenderTarget? _internal;

        public GfxBackBuffer() => throw new NotSupportedException();
        internal GfxBackBuffer(RHI.RenderTarget? buffer) => _internal = buffer;

        public void Dispose() => _internal?.Dispose();

        #region Base



        #endregion

        public nint Handle => NullableUtility.ThrowIfNull(_internal).Handle;

        public bool IsNull => _internal == null;
        public RHI.RenderTarget? RHIRenderTarget => _internal;
        public GfxResource Resource => new GfxResource(_internal);

        public static GfxBackBuffer Null = new GfxBackBuffer(null);

        public static explicit operator RHI.RenderTarget?(GfxBackBuffer backBuffer) => backBuffer._internal;
        public static implicit operator GfxBackBuffer(RHI.SwapChain? swapChain) => new GfxBackBuffer(swapChain?.BackBuffer);

        public static explicit operator GfxResource(GfxBackBuffer backBuffer) => new GfxResource(backBuffer._internal);
    }
}
