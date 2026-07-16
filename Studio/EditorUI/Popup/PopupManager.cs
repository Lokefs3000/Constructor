using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EditorUI.Popup.Menu;
using EditorUI.Styling;
using EditorUI.Windowing;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Input;
using Primary.Mathematics;
using Primary.Windowing;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

namespace EditorUI.Popup
{
    public sealed class PopupManager : IDisposable
    {
        private PopupWindowHost? _popupWindowHost;
        private Window? _popupWindow;

        private Window? _currentParentWindow;

        private bool _disposedValue;

        internal PopupManager()
        {
            _popupWindowHost = null;
            _popupWindow = null;

            _currentParentWindow = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _popupWindowHost?.Destroy();
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
            if (_popupWindowHost != null && _popupWindow != null && _popupWindowHost.Hosting != null)
            {
                _popupWindowHost.Hosting.UpdateSelf(_popupWindow);

                StateFlags menuStateFlags = _popupWindowHost.Hosting.StateFlags;
                if (menuStateFlags.HasFlags(StateFlags.InvalidStyle) || _popupWindowHost.StylesheetProvider.HasChangedStylesheets)
                    UIManager.Instance.StyleManager.UpdateSingleStyling(_popupWindowHost);
            }
        }

        [MemberNotNull(nameof(_popupWindow), nameof(_popupWindowHost))]
        private void CreateWindowIfNull()
        {
            _popupWindow ??= Primary.Windowing.WindowManager.Instance.CreateWindow("__POPUP", new Int2(200), CreateWindowFlags.Hidden | CreateWindowFlags.Borderless | CreateWindowFlags.Transparent);
            _popupWindow.IsModal = true;

            _popupWindowHost ??= new PopupWindowHost(_popupWindow);
        }

        private void CloseCurrentPopup()
        {
            if (_popupWindowHost?.Hosting != null)
            {
                _popupWindowHost.CleanupHosting();
            }
        }

        public void ClosePopup(PopupHost popupHost)
        {
            if (_popupWindowHost?.Hosting == popupHost)
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

        private void OpenPopup(PopupHost popupHost)
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

            if (_popupWindowHost?.Hosting != null)
                CloseCurrentPopup();

            CreateWindowIfNull();

            if (targetWindow != null)
            {
                _popupWindow.Parent = targetWindow;
                _currentParentWindow = targetWindow;

                targetWindow!.OnDestroy += OnDestroyParentWindowCallback;
            }

            _popupWindowHost.SetNewHosting(popupHost);
            _popupWindow.Show();
        }

        public void OpenMenu(PopupMenu popupMenu)
        {
            OpenPopup(popupMenu);
        }

        public DropdownMenuHost OpenDropdown(Int2 screenPosition, int menuWidth, ROList<string> options, int startIndex)
        {
            DropdownMenuHost dropdownMenu = new DropdownMenuHost(screenPosition, menuWidth, options, startIndex);
            OpenPopup(dropdownMenu);

            return dropdownMenu;
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

        internal PopupWindowHost? CurrentPopupHost => _popupWindowHost?.Hosting == null ? null : _popupWindowHost;
        internal Window? PopupWindow => _popupWindow;
    }
}
