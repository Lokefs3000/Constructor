using Primary.Editor;
using Primary.Rendering.Pass;

namespace Primary.Rendering
{
    public interface IRenderPath : IDisposable
    {
        public void PreparePasses(RenderPassData passData);
        public void ExecutePasses(RenderPass renderPass);

        public void EmitDebugStatistics(DebugDataContainer container);
    }
}
