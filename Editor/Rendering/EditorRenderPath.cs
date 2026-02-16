using Editor.Rendering.Passes;
using Editor.UI;
using Editor.UI.Visual;
using Primary.Rendering;

namespace Editor.Rendering
{
    internal sealed class EditorRenderPath : IRenderPath
    {
        private Gizmos? _gizmos;

        public void Install(RenderingManager manager)
        {
            RenderPassManager passes = manager.RenderPassManager;
            UIRenderer renderer = UIManager.Instance.Renderer;

            _gizmos = new Gizmos();

            passes.AddRenderPass<GizmoRenderPass>();
            renderer.InstallRenderPasses(passes);
            passes.AddRenderPass<DearImGuiRenderPass>();
        }

        public void Uinstall(RenderingManager manager)
        {
            RenderPassManager passes = manager.RenderPassManager;
            UIRenderer renderer = UIManager.Instance.Renderer;

            _gizmos!.Dispose();

            passes.RemoveRenderPass<GizmoRenderPass>();
            renderer.UninstallRenderPasses(passes);
            passes.RemoveRenderPass<DearImGuiRenderPass>();
        }

        public void PreRenderPassSetup(RenderingManager manager)
        {

        }
    }
}
