using Editor.UI.Diagnostics.Tabs;
using Editor.UI.Diagnostics.Views;
using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Diagnostics
{
    public sealed class DebugWindow : UIWindow
    {
        private IViewHost[] _views;

        private ITabHost[] _tabs;
        private int _tabIndex;

        private UIWindow? _currentWindow;

        private UIButton? _selectWindowButton;

        public DebugWindow(int uniqueWindowId) : base(uniqueWindowId)
        {
            WindowTitle = "Debug window";

            _views = [
                new SelectWindowView(this)
                ];

            _tabs = [
                new LayoutTab(this),
                new InteractionTab(this)
                ];

            _tabIndex = 1;

            _currentWindow = null;
        }

        protected override void InitializePostLoad()
        {
            _selectWindowButton = FindElementWithId<UIButton>("select-window-button");

            _selectWindowButton?.OnPressed += OnSelectWindowPressed;

            foreach (IViewHost view in _views)
            {
                view.Initialize();
            }

            foreach (ITabHost tab in _tabs)
            {
                tab.Initialize();
            }
        }

        protected override void CleanupSelf()
        {
            _selectWindowButton?.OnPressed -= OnSelectWindowPressed;

            _selectWindowButton = null;

            foreach (ITabHost tab in _tabs)
            {
                tab.Cleanup();
            }

            foreach (IViewHost view in _views)
            {
                view.Cleanup();
            }
        }

        private void OnSelectWindowPressed()
        {
            
        }

        internal UIWindow? ActiveWindow {
            get => _currentWindow;
            set
            {
                if (_currentWindow != value)
                {
                    _currentWindow = value;
                    OnActiveWindowChanged?.Invoke(value);
                }
            }
        }

        internal event Action<UIWindow?>? OnActiveWindowChanged;
    }
}
