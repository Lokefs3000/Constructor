using Primary.Rendering.Resources;

namespace Primary.Rendering.NRD
{
    public interface INativeRenderDispatcher : IDisposable
    {
        public void Dispatch(RenderPassManager manager);

        public NRDResourceInfo QueryResourceInfo(FrameGraphResource resource);
        public NRDResourceInfo QueryBufferInfo(FrameGraphBuffer buffer, int offset, int size);
        public NRDResourceInfo QueryTextureInfo(FrameGraphTexture texture, int offset, int size);
    }

    public readonly record struct NRDResourceInfo(int SizeInBytes, int Alignment);
}
