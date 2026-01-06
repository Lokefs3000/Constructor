using SDL;
using System.Numerics;

namespace Primary.Rendering
{
    public class WindowManager : IDisposable
    {
        private Window? _primaryWindow;
        private Dictionary<SDL_WindowID, Window> _windows;

        private bool _disposedValue;

        internal WindowManager()
        {
            s_instance = this;

            _primaryWindow = null;
            _windows = new Dictionary<SDL_WindowID, Window>();

            //ExceptionUtility.Assert(SDL3.SDL_SetHint(SDL3.SDL_HINT_MOUSE_AUTO_CAPTURE, true));
        }

        public Window CreateWindow(string windowTitle, Vector2 clientSize, CreateWindowFlags flags)
        {
            Window window = new Window(windowTitle, clientSize, flags);
            _windows.Add(window.ID, window);

            if (_primaryWindow == null)
                _primaryWindow = window;
            return window;
        }

        public void DestroyWindow(Window window)
        {
            WindowDestroyed?.Invoke(window);

            _windows.Remove(window.ID);

            window.Dispose();

            if (_primaryWindow == window)
                _primaryWindow = _windows.Count > 0 ? _windows.Values.GetEnumerator().Current : null;
        }

        public Window? FindWindow(SDL_WindowID id)
        {
            if (_windows.TryGetValue(id, out Window? value))
                return value;
            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (Window window in _windows.Values)
                    {
                        window.Dispose();
                    }

                    _windows.Clear();
                }

                s_instance = null;

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Window? PrimaryWindow => _primaryWindow;

        public event Action<Window>? WindowDestroyed;

        private static WindowManager? s_instance;
        public static WindowManager Instance => s_instance!;
    }
}
