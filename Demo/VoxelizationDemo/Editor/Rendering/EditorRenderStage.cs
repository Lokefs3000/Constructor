using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering;

namespace VoxelizationDemo.Editor.Rendering
{
    internal sealed class EditorRenderStage : IRenderStage
    {
        private DearImGuiRenderPass? _dearImGuiRenderPass;
        private DrawWorldIconsPass? _drawWorldIconsPass;
        private InteractionGizmoRenderPass? _drawInteractionGizmoRenderPass;
        private PickingRenderPass? _pickingRenderPass;

        public void Install(RenderingManager manager)
        {
            _dearImGuiRenderPass = new DearImGuiRenderPass();
            _drawWorldIconsPass = new DrawWorldIconsPass();
            _drawInteractionGizmoRenderPass = new InteractionGizmoRenderPass();
            _pickingRenderPass = new PickingRenderPass();
        }

        public void Uninstall(RenderingManager manager)
        {
            _dearImGuiRenderPass?.Dispose();
            _dearImGuiRenderPass = null;

            _drawWorldIconsPass = null;
            _drawInteractionGizmoRenderPass = null;
            _pickingRenderPass = null;
        }

        public void FlushPassesForCamera(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            manager.PushRenderPass(_pickingRenderPass!);
            manager.PushRenderPass(_drawWorldIconsPass!);
            manager.PushRenderPass(_drawInteractionGizmoRenderPass!);
        }

        public void FlushPassesForWindow(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            manager.PushRenderPass(_dearImGuiRenderPass!);
        }
    }
}
