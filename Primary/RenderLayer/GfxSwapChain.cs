using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RenderLayer
{
    public record struct GfxSwapChain : IDisposable
    {
        private RHI.SwapChain? _internal;

        public GfxSwapChain() => throw new NotSupportedException();
        internal GfxSwapChain(RHI.SwapChain? swapChain) => _internal = swapChain;

        public void Dispose() => _internal?.Dispose();

        #region Base

        public void Present(RHI.PresentParameters parameters) => NullableUtility.ThrowIfNull(_internal).Present(parameters);
        public bool Resize(Vector2 newClientSize) => NullableUtility.ThrowIfNull(_internal).Resize(newClientSize);

        #endregion

        public Vector2 ClientSize => NullableUtility.ThrowIfNull(_internal).ClientSize;
        public GfxBackBuffer BackBuffer => new GfxBackBuffer(NullableUtility.ThrowIfNull(_internal).BackBuffer);

        public bool IsNull => _internal == null;
        public RHI.SwapChain? RHISwapChain => _internal;

        public static GfxSwapChain Null = new GfxSwapChain(null);

        public static explicit operator RHI.SwapChain?(GfxSwapChain swapChain) => swapChain._internal;
        public static implicit operator GfxSwapChain(RHI.SwapChain? swapChain) => new GfxSwapChain(swapChain);
    }
}
