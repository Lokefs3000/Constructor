using Primary.Editor;
using Primary.Rendering.Data;

namespace Primary.Rendering
{
    public interface IRenderPath : IDisposable
    {
        public void PreparePasses(RenderPassData passData);
        public void CleanupPasses(RenderPassData passData);

        public void EmitDebugStatistics(DebugDataContainer container);
    }
}
