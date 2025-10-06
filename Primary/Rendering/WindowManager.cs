using SDL;
using System.Numerics;

namespace Primary.Rendering
{
    public class WindowManager : IDisposable
    {
        private Dictionary<SDL_WindowID, Window> _windows;

        private bool _disposedValue;

        internal WindowManager()
        {
            s_instance = this;

            _windows = new Dictionary<SDL_WindowID, Window>();

            //ExceptionUtility.Assert(SDL3.SDL_SetHint(SDL3.SDL_HINT_MOUSE_AUTO_CAPTURE, true));
        }

        public Window CreateWindow(string windowTitle, Vector2 clientSize, CreateWindowFlags flags)
        {
            Window window = new Window(windowTitle, clientSize, flags);
            _windows.Add(window.ID, window);

            return window;
        }

        public void DestroyWindow(Window window)
        {
            _windows.Remove(window.ID);

            window.Dispose();
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

        private static WindowManager? s_instance;
        public static WindowManager Instance => s_instance!;
    }
}
