using Primary.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Debuggable
{
    public sealed class DebugManager
    {
        private bool _isEnabled;

        private StatisticDrawer _statisticsDrawer;

        internal DebugManager()
        {
            _isEnabled = false;

            _statisticsDrawer = new StatisticDrawer();

            if (AppArguments.HasArgument("--dbgrender"))
            {
                // delay until full initialization has occured
                ThreadHelper.ExecuteOnMainThread(Enable);
            }
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
