using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.UI
{
    public sealed class UIWindowManager : IDisposable
    {
        private Dictionary<int, UIWindow> _activeWindows;

        private IWindow? _windowWithFocus;

        private bool _disposedValue;

        internal UIWindowManager()
        {
            _activeWindows = new Dictionary<int, UIWindow>();

            _windowWithFocus = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    FocusWindow(null);

                    foreach (var (id, window) in _activeWindows)
                    {
                        window.Cleanup();
                    }

                    _activeWindows.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private int GetNewWindowId()
        {
            int id = (int)Stopwatch.GetTimestamp();
            while (_activeWindows.ContainsKey(id) && id != int.MaxValue)
            {
                id = (int)Stopwatch.GetTimestamp();
            }

            return id;
        }

        internal T OpenWindow<T>() where T : UIWindow
        {
            T window = (T)Activator.CreateInstance(typeof(T), [GetNewWindowId()])!;

            _activeWindows.Add(window.UniqueWindowId, window);
            return window;
        }

        internal void FocusWindow(IWindow? window)
        {
            if (_windowWithFocus == window)
                return;

            _windowWithFocus?.OnFocusLostCallback();
            window?.OnFocusGainedCallback();

            _windowWithFocus = window;
        }

        internal T? FindWindow<T>() where T : UIWindow
        {
            Type t = typeof(T);
            foreach (var kvp in _activeWindows)
            {
                if (kvp.Value.GetType() == t)
                    return kvp.Value as T;
            }

            return null;
        }

        public Dictionary<int, UIWindow> Active => _activeWindows;
        public IWindow? FocusedWindow => _windowWithFocus;
    }
}
