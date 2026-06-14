using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Diagnostics.Tabs
{
    internal sealed class LayoutTab : ITabHost
    {
        private readonly DebugWindow _window;

        internal LayoutTab(DebugWindow window)
        {
            _window = window;
        }

        public void Initialize()
        {
            if (_window.ActiveWindow != null)
                OnActiveWindowChanged(_window.ActiveWindow);

            _window.OnActiveWindowChanged += OnActiveWindowChanged;
        }

        public void Cleanup()
        {
            OnActiveWindowChanged(null);

            _window.OnActiveWindowChanged -= OnActiveWindowChanged;
        }

        private void OnActiveWindowChanged(UIWindow? newWindow)
        {
            if (newWindow != null)
            {

            }
        }
    }
}
