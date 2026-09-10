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
        }

        internal void TrySetupAfterRenderInit(RenderingManager renderer)
        {
            if (_gizmos != null && _screenGizmos != null)
            {
                renderer.RenderPassManager.AddRenderPass<GizmoRenderPass>();
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
        }

        public void Disable()
        {
            if (!_isEnabled)
                return;

            Engine engine = Engine.GlobalSingleton;
            engine.ImGuiManager.RemoveDrawer(_statisticsDrawer);
        }
    }
}
