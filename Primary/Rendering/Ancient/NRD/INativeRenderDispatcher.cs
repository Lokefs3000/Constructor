using Primary.Rendering.Ancient;
using Primary.Rendering.Ancient.Resources;

namespace Primary.Rendering.Ancient.NRD
{
    public interface INativeRenderDispatcher : IDisposable
    {
        public void Dispatch(RenderPassManager manager);

        public NRDResourceInfo QueryResourceInfo(FGResource resource);
        public NRDResourceInfo QueryBufferInfo(FGBuffer buffer, int offset, int size);
        public NRDResourceInfo QueryTextureInfo(FGTexture texture, int offset, int size);
    }

    public readonly record struct NRDResourceInfo(int SizeInBytes, int Alignment);
}
