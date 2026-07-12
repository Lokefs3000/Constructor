using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Visual.Passes;
using Primary.GUI.ImGui;
using Primary.Rendering;
using PrimaryEditor.Rendering.Passes;
using PrimaryEditor.Rendering.UI;

namespace PrimaryEditor.Rendering
{
    public sealed class EditorRenderPath : IRenderPath
    {
        public void PreRenderPassSetup(RenderingManager manager)
        {

        }

        public void Install(RenderingManager manager)
        {
            manager.RenderPassManager.AddRenderPass<UpdateFontsPass>();
            manager.RenderPassManager.AddRenderPass<RenderUIPass>();
            manager.RenderPassManager.AddRenderPass<GizmoRenderPass>();
            manager.RenderPassManager.AddRenderPass<ImGuiRenderPass>();
        }

        public void Uninstall(RenderingManager manager)
        {
            
        }
    }
}
