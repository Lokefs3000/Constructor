using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EditorUI.Popup.Menu;
using EditorUI.Styling;
using EditorUI.Windowing;
using Primary.Common;
using Primary.Input;
using Primary.Mathematics;
using Primary.Windowing;

namespace EditorUI.Popup
{
    public sealed class PopupManager : IDisposable
    {
        private PopupHost? _currentPopup;
        private Window? _popupWindow;

        private Window? _currentParentWindow;

        private bool _disposedValue;

        internal PopupManager()
        {
            _currentPopup = null;
            _popupWindow = null;

            _currentParentWindow = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _currentPopup?.Destroy();
                    _popupWindow?.Dispose();

                    _currentParentWindow?.OnDestroy -= OnDestroyParentWindowCallback;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Update()
        {
            if (_currentPopup != null && _popupWindow != null)
            {
                _currentPopup.Update();

                if (_currentPopup is ISingleStyledObject singleStyledObject)
                {
                    StateFlags menuStateFlags = _currentPopup.MenuStateFlags;
                    if (menuStateFlags.HasFlags(StateFlags.InvalidStyle) || singleStyledObject.StylesheetProvider.HasChangedStylesheets)
                        UIManager.Instance.StyleManager.UpdateSingleStyling(singleStyledObject);
                }
            }
        }

        [MemberNotNull(nameof(_popupWindow))]
        private void CreateWindowIfNull()
        {
            _popupWindow ??= Primary.Windowing.WindowManager.Instance.CreateWindow("__POPUP", new Int2(200), CreateWindowFlags.Hidden | CreateWindowFlags.Borderless | CreateWindowFlags.Transparent);
            _popupWindow.IsModal = true;
        }

        private void CloseCurrentPopup()
        {
            if (_currentPopup != null)
            {
                _currentPopup.Destroy();
                _currentPopup = null;
            }
        }

        public void OpenMenu(PopupMenu popupMenu)
        {
            Window? targetWindow = null;

            WindowBase? currentWindowFocus = UIManager.Instance.WindowManager.CurrentWindowFocus;
            if (currentWindowFocus?.Parent?.Host != null)
            {
                targetWindow = currentWindowFocus.Parent.Host.OwnedWindow;
            }
            else
            {
                targetWindow = Primary.Windowing.WindowManager.Instance.ActiveWindow;
            }

            UIManager.Instance.ActionScheduler.TryScheduleUnique(this, static (key, argsRaw) =>
            {
                PopupManager @this = (PopupManager)key;
                PopupMenuArgs args = (PopupMenuArgs)argsRaw!;

                if (@this._currentPopup != null)
                    @this.CloseCurrentPopup();

                @this.CreateWindowIfNull();

                if (args.ParentWindow != null && !args.ParentWindow.IsDestroyed)
                {
                    @this._popupWindow.Parent = args.ParentWindow;
                    @this._currentParentWindow = args.ParentWindow;

                    args.ParentWindow!.OnDestroy += @this.OnDestroyParentWindowCallback;
                }

                @this._currentPopup = new PopupMenuHost(args.Menu, @this._popupWindow);

                @this._popupWindow.Position = InputSystem.Pointer.GlobalMousePosition;
                @this._popupWindow.Show();
            }, new PopupMenuArgs(popupMenu, targetWindow));
        }

        public void CloseMenu(PopupMenu popupMenu)
        {
            if (_currentPopup is PopupMenuHost menuHost && menuHost.HostedPopupMenu == popupMenu)
            {
                CloseCurrentPopup();

                if (_popupWindow != null)
                {
                    _popupWindow.Hide();
                    _popupWindow.Parent = null;
                }

                _currentParentWindow?.OnDestroy -= OnDestroyParentWindowCallback;
                _currentParentWindow = null;
            }
        }

        private void OnDestroyParentWindowCallback(Window window)
        {
            if (_popupWindow != null && _popupWindow.Parent == window)
            {
                _popupWindow.Parent = null;
                CloseCurrentPopup();

                UILog.Logger?.Information("Popup window parent '{w}' was destroyed while being referenced", window);
            }
        }

        internal PopupHost? CurrentPopupHost => _currentPopup;
        internal Window? PopupWindow => _popupWindow;

        private record class PopupMenuArgs(PopupMenu Menu, Window? ParentWindow);
    }
}
