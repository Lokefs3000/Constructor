using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Input;
using EditorUI.Popup.Menu;
using EditorUI.Styling;
using EditorUI.Text;
using EditorUI.Visual;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Timing;
using Primary.Windowing;

namespace EditorUI.Popup
{
    internal sealed class PopupMenuHost : PopupHost, ISingleStyledObject, IInputDispatcher
    {
        private readonly PopupMenu _popupMenu;
        private readonly Window _window;

        internal PopupMenuHost(PopupMenu popupMenu, Window window)
        {
            _popupMenu = popupMenu;
            _window = window;

            popupMenu.Lock();
            UIManager.Instance.InputManager.BindInputDispatcher(window, this);
        }

        protected internal override void Destroy()
        {
            UIManager.Instance.InputManager.UnbindInputDispatcher(_window, this);
            _popupMenu.Unlock();
        }

        protected internal override void Update()
        {
            _popupMenu.UpdateSelf(_window);
        }

        protected internal override void Paint(ref readonly PainterContext painter)
        {
            if (_window == null)
                return;

            _popupMenu.PaintSelf(in painter, Vector2.Zero, _window);
        }

        #region ISingleStyledObject
        StylesheetProvider ISingleStyledObject.StylesheetProvider => _popupMenu.StylesheetProvider;

        StyledObject ISingleStyledObject.StyledObject => _popupMenu;
        bool ISingleStyledObject.GetAllProperties => false;
        #endregion
        #region IInputDispatcher
        public InputDispatcherRoot GetInteractable(Vector2 point)
        {
            return new InputDispatcherRoot(_popupMenu, Vector2.Zero);
        }

        public Window Window => _window;
        #endregion

        public PopupMenu HostedPopupMenu => _popupMenu;

        protected internal override StateFlags MenuStateFlags => _popupMenu.StateFlags;
    }
}
