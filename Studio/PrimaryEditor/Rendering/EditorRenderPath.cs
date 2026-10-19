using System;
using System.Collections.Generic;
using System.Text;
using Primary.GUI.ImGui;
using Primary.Rendering;
using PrimaryEditor.Rendering.UI;

namespace PrimaryEditor.Rendering
{
    public sealed class EditorRenderPath : IRenderStage
    {
        //private UpdateFontsPass? _fontsPass;
        //private RenderUIPass? _renderUIPass;
        //private GizmoRenderPass? _gizmoRenderPass;
        private ImGuiRenderPass? _imGuiRenderPass;

        public void Install(RenderingManager manager)
        {
            //_fontsPass = new UpdateFontsPass();
            //_renderUIPass = new RenderUIPass();
            //_gizmoRenderPass = new GizmoRenderPass();
            _imGuiRenderPass = new ImGuiRenderPass();
        }

        public void Uninstall(RenderingManager manager)
        {
            //_fontsPass = null;
            //_renderUIPass = null;
            //_gizmoRenderPass = null;
            _imGuiRenderPass = null;
        }

        public void FlushPassesForCamera(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            //manager.PushRenderPass(_gizmoRenderPass!);
            manager.PushRenderPass(_imGuiRenderPass!);
        }

        public void FlushPassesForWindow(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            //manager.PushRenderPass(_fontsPass!);
            //manager.PushRenderPass(_renderUIPass!);
        }
    }
}
