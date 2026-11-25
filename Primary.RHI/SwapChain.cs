using System.Numerics;

namespace Primary.RHI
{
    public abstract class SwapChain : IDisposable
    {
        public abstract Vector2 ClientSize { get; }
        public abstract RenderTarget BackBuffer { get; }

        public abstract Texture NewFGCompatTexture { get; }

        public abstract void Dispose();

        public abstract void Present(PresentParameters parameters);
        public abstract bool Resize(Vector2 newClientSize);
    }

    [Flags]
    public enum PresentParameters : byte
    {
        None = 0,
        VSync = 1 << 0
    }
}
