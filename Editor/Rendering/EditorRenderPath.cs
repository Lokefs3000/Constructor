using Editor.DearImGui;
using Primary.Rendering2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Rendering
{
    internal sealed class EditorRenderPath : IRenderPath
    {
        public void Install(RenderingManager manager)
        {
            RenderPassManager passes = manager.RenderPassManager;

            passes.AddRenderPass<DearImGuiRenderPass>();
        }

        public void Uinstall(RenderingManager manager)
        {
            
        }

        public void PreRenderPassSetup(RenderingManager manager)
        {

        }
    }
}
