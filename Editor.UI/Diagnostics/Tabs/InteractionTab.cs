using Editor.UI.Elements;
using Editor.UI.Interaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Diagnostics.Tabs
{
    internal sealed class InteractionTab : ITabHost
    {
        private readonly DebugWindow _window;

        private UIElement? _contentElement;

        internal InteractionTab(DebugWindow window)
        {
            _window = window;
        }

        public void Initialize()
        {
            _contentElement = _window.FindElementWithId<UIElement>("interaction-content");

            if (_window.ActiveWindow != null)
                OnActiveWindowChanged(_window.ActiveWindow);

            _window.OnActiveWindowChanged += OnActiveWindowChanged;
        }

        public void Cleanup()
        {
            _contentElement = null;

            OnActiveWindowChanged(null);

            _window.OnActiveWindowChanged -= OnActiveWindowChanged;
        }

        private void OnActiveWindowChanged(UIWindow? newWindow)
        {
            if (_contentElement != null)
            {
                if (newWindow != null && newWindow.ParentHost != null)
                {
                    HostInteractionManager interactionManager = newWindow.ParentHost.InteractionManager;
                    
                }
            }
        }
    }
}
