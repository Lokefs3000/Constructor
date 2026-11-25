using Editor.UI.Assets;
using Primary.Input;
using Primary.Mathematics;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Menu
{
    public sealed class ContextMenu : IDisposable
    {
        private Window _contextMenuWindow;
        private ContextMenuHost? _currentHost;

        private bool _disposedValue;

        internal ContextMenu()
        {
            _contextMenuWindow = WindowManager.Instance.CreateWindow("CONTEXTMENU", Int2.One, CreateWindowFlags.Hidden | CreateWindowFlags.Borderless | CreateWindowFlags.AlwaysOnTop);
            _currentHost = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_currentHost != null)
                        CloseCurrentContextMenu();
                    WindowManager.Instance.DestroyWindow(_contextMenuWindow);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal ContextMenuHost OpenNewHost(ContextMenuAsset asset, IContextMenuListener listener)
        {
            if (_currentHost != null)
                _currentHost.Close();

            _currentHost = new ContextMenuHost(_contextMenuWindow, InputSystem.Pointer.GlobalMousePosition.AsVector2(), asset, listener);
            return _currentHost;
        }

        internal void CloseCurrentContextMenu()
        {
            if (_currentHost != null)
            {
                _contextMenuWindow.Hide();
                _contextMenuWindow.ClientSize = Int2.One;

                _currentHost.Cleanup();
                _currentHost = null;
            }
        }

        public static ContextMenuHost Open(ContextMenuAsset asset, IContextMenuListener listener) => UIManager.Instance.ContextMenu.OpenNewHost(asset, listener);
        public static void Close() => UIManager.Instance.ContextMenu.CloseCurrentContextMenu();
    }
}
