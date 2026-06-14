using Editor.Rendering.Passes;
using Editor.UI;
using Editor.UI.Visual;
using Primary.Profiling;
using Primary.R2.ForwardPlus;
using Primary.Rendering;

namespace Editor.Rendering
{
    internal sealed class EditorRenderPath : IRenderPath
    {
        private ForwardPlusRenderPath? _forwardPlusRenderPath;

        public void Install(RenderingManager manager)
        {
            RenderPassManager passes = manager.RenderPassManager;
            UIRenderer renderer = UIManager.Instance.Renderer;

            _forwardPlusRenderPath = new ForwardPlusRenderPath();

            _forwardPlusRenderPath.Install(manager);

            //passes.AddRenderPass<GizmoRenderPass>();
            renderer.InstallRenderPasses(passes);
            passes.AddRenderPass<DearImGuiRenderPass>();
        }

        public void Uninstall(RenderingManager manager)
        {
            RenderPassManager passes = manager.RenderPassManager;
            UIRenderer renderer = UIManager.Instance.Renderer;

            _forwardPlusRenderPath?.Uninstall(manager);

            //passes.RemoveRenderPass<GizmoRenderPass>();
            renderer.UninstallRenderPasses(passes);
            passes.RemoveRenderPass<DearImGuiRenderPass>();
        }

        public void PreRenderPassSetup(RenderingManager manager)
        {
            using (new ProfilingScope("ForwardPlus"))
            {
                _forwardPlusRenderPath?.PreRenderPassSetup(manager);
            }
        }
    }
}
