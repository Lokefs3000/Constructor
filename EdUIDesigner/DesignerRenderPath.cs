using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace EdUIDesigner
{
    internal sealed class DesignerRenderPath : IRenderPath
    {
        private readonly Designer _designer;

        internal DesignerRenderPath(Designer designer)
        {
            _designer = designer;
        }

        public void Install(RenderingManager manager)
        {
            _designer.UIManager.Renderer.InstallRenderPasses(manager.RenderPassManager);
        }

        public void Uinstall(RenderingManager manager)
        {
            _designer.UIManager.Renderer.UninstallRenderPasses(manager.RenderPassManager);
        }

        public void PreRenderPassSetup(RenderingManager manager) { }
    }
}
