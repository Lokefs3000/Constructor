using Primary.Rendering.Data;

namespace Primary.Rendering
{
    public interface IRenderPass : IDisposable
    {
        public void PrepareFrame(IRenderPath path, RenderPassData passData);
        public void ExecutePass(IRenderPath path, RenderPassData passData);
        public void CleanupFrame(IRenderPath path, RenderPassData passData);
    }
}
