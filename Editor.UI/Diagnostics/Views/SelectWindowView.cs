using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Primary.Input.Devices;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Diagnostics.Views
{
    internal sealed class SelectWindowView : IViewHost
    {
        private readonly DebugWindow _window;

        private UILabel? _activeWindowLabel;
        private UIButton? _selectWindowButton;

        private UIElement? _popupRoot;
        private UIElement? _popupListView;
        private UIButton? _popupGoBackButton;

        private Dictionary<UIWindow, SelectableWindow> _windowsInList;

        internal SelectWindowView(DebugWindow window)
        {
            _window = window;

            _windowsInList = new Dictionary<UIWindow, SelectableWindow>();
        }

        public void Initialize()
        {
            UIManager manager = UIManager.Instance;

            _activeWindowLabel = _window.FindElementWithId<UILabel>("active-window-label");
            _selectWindowButton = _window.FindElementWithId<UIButton>("select-window-button");

            _popupRoot = _window.FindElementWithId<UIElement>("select-window-popup");
            _popupListView = _popupRoot?.FindElementWithId<UIElement>("select-window-view");
            _popupGoBackButton = _popupRoot?.FindElementWithId<UIButton>("select-window-goback");

            if (_activeWindowLabel != null && _window.ActiveWindow != null)
            {
                UIWindow window = _window.ActiveWindow;

                _activeWindowLabel.Text = @$"{window.WindowTitle} <i><color=""#808080"">({window.UniqueWindowId})";
                _activeWindowLabel.RemoveClass(InactiveLabelClassName);
            }

            _window.OnActiveWindowChanged += OnActiveWindowChanged;
            _selectWindowButton?.OnPressed += OpenSelectWindowPopup;
            _popupGoBackButton?.OnPressed += CloseSelectWindowPopup;

            manager.OnWindowClosed += OnWindowClosed;
        }

        public void Cleanup()
        {
            UIManager manager = UIManager.Instance;

            _activeWindowLabel = null;
            _selectWindowButton = null;

            _popupRoot = null;
            _popupListView = null;
            _popupGoBackButton = null;

            foreach (var (key, value) in _windowsInList)
            {
                value.Dispose();
            }

            _windowsInList.Clear(); // auto-destroyed because they are part of the tree
            
            _window.OnActiveWindowChanged -= OnActiveWindowChanged;

            manager.OnWindowOpened -= OnWindowOpened;
            manager.OnWindowClosed -= OnWindowClosed;
        }

        private void OnActiveWindowChanged(UIWindow? newWindow)
        {
            if (_activeWindowLabel != null)
            {
                if (_window.ActiveWindow != null)
                {
                    UIWindow window = _window.ActiveWindow;

                    _activeWindowLabel.Text = @$"{window.WindowTitle} <i><color=""#808080"">({window.UniqueWindowId})";
                    _activeWindowLabel.RemoveClass(InactiveLabelClassName);
                }
                else
                {
                    _activeWindowLabel.Text = "No active window";
                    _activeWindowLabel.AddClass(InactiveLabelClassName);
                }
            }
        }

        private void OpenSelectWindowPopup()
        {
            _popupRoot?.IsEnabled = true;

            UIManager manager = UIManager.Instance;
            foreach (var (id, window) in manager.WindowManager.Active)
            {
                OnWindowOpened(window);
            }

            manager.OnWindowOpened += OnWindowOpened;
        }

        private void CloseSelectWindowPopup()
        {
            _popupRoot?.IsEnabled = false;

            UIManager manager = UIManager.Instance;
            manager.OnWindowOpened -= OnWindowOpened;
        }

        private void OnWindowOpened(UIWindow window)
        {
            if (_popupListView != null && !_windowsInList.ContainsKey(window))
            {
                UIButton button = new UIButton() { Parent = _popupListView };
                button.AddClass("sw-popup-list-button");

                if (window == _window.ActiveWindow)
                    button.AddClass(CurrentActiveWindowClassName);

                UILabel label = new UILabel
                {
                    Parent = button,

                    IsActive = false,
                    Text = window.WindowTitle,
                };
                label.AddClass("sw-popup-list-label");

                SelectableWindow selectableWindow = new SelectableWindow(this, window, button, label);
                _windowsInList.Add(window, selectableWindow);
            }
        }

        private void OnWindowClosed(UIWindow window)
        {
            if (_windowsInList.Remove(window, out SelectableWindow? selectableWindow))
            {
                selectableWindow.Dispose();
            }
        }

        private void OnWindowButtonPressed(SelectableWindow selectableWindow)
        {
            _window.ActiveWindow = selectableWindow.Window;
            CloseSelectWindowPopup();
        }

        private const string InactiveLabelClassName = "no-active-window";
        private const string CurrentActiveWindowClassName = "sw-popup-current-window";

        private sealed class SelectableWindow : IDisposable
        {
            private readonly SelectWindowView _owner;
            private readonly UIWindow _window;
            private readonly UIButton _button;
            private readonly UILabel _label;

            internal SelectableWindow(SelectWindowView owner, UIWindow window, UIButton button, UILabel label)
            {
                _owner = owner;
                _window = window;
                _button = button;
                _label = label;

                window.OnWindowTitleChanged += OnWindowTitleChanged;
                button.OnPressed += OnButtonPressed;
                owner._window.OnActiveWindowChanged += OnActiveWindowChanged;
            }

            public void Dispose()
            {
                _button.OnPressed -= OnButtonPressed;
                _window.OnWindowTitleChanged -= OnWindowTitleChanged;
                _owner._window.OnActiveWindowChanged -= OnActiveWindowChanged;

                _button.Destroy();
            }

            private void OnWindowTitleChanged(string newTitle)
            {
                _label.Text = newTitle;
            }

            private void OnButtonPressed()
            {
                _owner.OnWindowButtonPressed(this);
            }

            private void OnActiveWindowChanged(UIWindow? window)
            {
                if (window == _window)
                    _button.AddClass(CurrentActiveWindowClassName);
                else
                    _button.RemoveClass(CurrentActiveWindowClassName);
            }

            public UIWindow Window => _window;
        }
    }
}
