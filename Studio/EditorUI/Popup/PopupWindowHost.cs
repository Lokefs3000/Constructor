using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using EditorUI.Input;
using EditorUI.Styling;
using Primary.Assets;
using Primary.Windowing;

namespace EditorUI.Popup
{
    internal sealed class PopupWindowHost : ISingleStyledObject, IInputDispatcher
    {
        private readonly StylesheetProvider _stylesheetProvider;
        private readonly Window _popupWindow;

        private PopupHost? _hosting;

        internal PopupWindowHost(Window window)
        {
            _stylesheetProvider = new StylesheetProvider(UIManager.Instance.ValueSerializer);
            _popupWindow = window;

            _hosting = null;
            UIManager.Instance.InputManager.BindInputDispatcher(_popupWindow, this);
        }

        internal void Destroy()
        {
            if (_hosting != null)
                CleanupHosting();

            _stylesheetProvider.Dispose();
            UIManager.Instance.InputManager.UnbindInputDispatcher(_popupWindow, this);
        }

        internal void CleanupHosting()
        {
            if (_hosting != null)
            {
                _hosting.CleanupSelf();
                _hosting = null;
            }
        }

        internal void SetNewHosting(PopupHost host)
        {
            Debug.Assert(_hosting == null);
            _hosting = host;

            host.StartHostingSelf(_popupWindow);
        }

        public InputDispatcherRoot GetInteractable(Vector2 point)
        {
            if (_hosting == null || !(_hosting.Shape?.Intersects(point) ?? true))
                return InputDispatcherRoot.Null;
            return new InputDispatcherRoot(_hosting?.GetInteractable(point), Vector2.Zero);
        }

        internal PopupHost? Hosting => _hosting;

        public StyledObject? StyledObject => _hosting;
        public StylesheetProvider StylesheetProvider => _stylesheetProvider;

        public bool GetAllProperties => false;

        public Window Window => _popupWindow;
    }
}
