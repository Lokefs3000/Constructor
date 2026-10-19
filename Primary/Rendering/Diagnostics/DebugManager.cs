using Primary.Rendering;
using Primary.Rendering.Diagnostics.Passes;
using Primary.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Diagnostics
{
    public sealed class DebugManager
    {
        private bool _isEnabled;

        private readonly StatisticDrawer _statisticsDrawer;

        private readonly Gizmos? _gizmos;
        private readonly ScreenGizmos? _screenGizmos;

        private GizmoRenderPass? _gizmoRenderPass;

        internal DebugManager()
        {
            _isEnabled = false;

            _statisticsDrawer = new StatisticDrawer();

            if (AppArguments.HasArgument("--render-draw-gizmos"))
            {
                _gizmos = new Gizmos();
                _screenGizmos = new ScreenGizmos();
            }

            if (AppArguments.HasArgument("--dbgrender"))
            {
                // delay until full initialization has occured
                ThreadHelper.ExecuteOnMainThread(Enable);
            }

            _gizmoRenderPass = null;
        }

        internal void TrySetupAfterRenderInit(RenderingManager renderer)
        {
            if (_gizmos != null && _screenGizmos != null)
            {
                //renderer.RenderPassManager.AddRenderPass<GizmoRenderPass>();
            }
        }

        internal void FinishCurrentRender()
        {
            _gizmos?.ClearDrawData();
            _screenGizmos?.ClearDrawData();
        }

        public void Enable()
        {
            if (_isEnabled)
                return;

            Engine engine = Engine.GlobalSingleton;
            engine.ImGuiManager.AddDrawer(_statisticsDrawer);

            _gizmoRenderPass = new GizmoRenderPass();

            _isEnabled = true;
        }

        public void Disable()
        {
            if (!_isEnabled)
                return;

            Engine engine = Engine.GlobalSingleton;
            engine.ImGuiManager.RemoveDrawer(_statisticsDrawer);

            _gizmoRenderPass?.Dispose();
            _gizmoRenderPass = null;

            _isEnabled = false;
        }

        internal void UpdatePerCamera(RenderingManager renderer)
        {
            if (_isEnabled && _gizmoRenderPass != null)
            {
                renderer.PushRenderPass(_gizmoRenderPass);
            }
        }
    }
}
