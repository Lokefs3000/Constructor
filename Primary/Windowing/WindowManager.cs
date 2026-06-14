using Primary.Mathematics;
using Primary.Polling;
using SDL;

using static SDL.SDL_EventType;

namespace Primary.Windowing
{
    public class WindowManager : IDisposable, IEventHandler
    {
        private Window? _primaryWindow;

        private Dictionary<uint, Window> _windows;
        private Dictionary<uint, Display> _displays;

        private bool _disposedValue;

        internal WindowManager()
        {
            s_instance = this;

            _primaryWindow = null;

            _windows = new Dictionary<uint, Window>();
            _displays = new Dictionary<uint, Display>();

            //ExceptionUtility.Assert(SDL3.SDL_SetHint(SDL3.SDL_HINT_MOUSE_AUTO_CAPTURE, true));

            {
                using SDLArray<SDL_DisplayID>? ids = SDL3.SDL_GetDisplays();
                if (ids != null)
                {
                    foreach (SDL_DisplayID id in ids)
                    {
                        Display display = new Display(id);
                        _displays.Add((uint)id, display);
                    }
                }
            }

            Engine.GlobalSingleton.EventManager.AddHandler(this);
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

        public void Handle(ref readonly SDL_Event @event)
        {
            switch (@event.Type)
            {
                case SDL_EVENT_DISPLAY_ADDED:
                    {
                        Display display = new Display(@event.display.displayID);
                        _displays.Add((uint)@event.display.displayID, display);
                        break;
                    }
                case SDL_EVENT_DISPLAY_REMOVED:
                    {
                        _displays.Remove((uint)@event.display.displayID);
                        break;
                    }
                case SDL_EVENT_DISPLAY_MOVED:
                    {
                        if (_displays.TryGetValue((uint)@event.display.displayID, out Display? display))
                            display.FetchBoundaries(true);
                        break;
                    }
                case SDL_EVENT_DISPLAY_USABLE_BOUNDS_CHANGED:
                    {
                        if (_displays.TryGetValue((uint)@event.display.displayID, out Display? display))
                            display.FetchBoundaries(true);
                        break;
                    }
            }
        }

        public Window CreateWindow(string windowTitle, Int2 clientSize, CreateWindowFlags flags)
        {
            Window window = new Window(windowTitle, clientSize, flags);
            _windows.Add(window.WindowId, window);

            if (_primaryWindow == null)
                _primaryWindow = window;
            return window;
        }

        public void DestroyWindow(Window window)
        {
            WindowDestroyed?.Invoke(window);

            _windows.Remove(window.WindowId);

            window.Dispose();

            if (_primaryWindow == window)
                _primaryWindow = _windows.Count > 0 ? _windows.Values.GetEnumerator().Current : null;
        }

        public Window? FindWindow(uint id)
        {
            if (_windows.TryGetValue(id, out Window? value))
                return value;
            return null;
        }

        public unsafe Display? GetDisplayForPoint(Int2 point)
        {
            SDL_DisplayID id = SDL3.SDL_GetDisplayForPoint((SDL_Point*)&point);

            if (_displays.TryGetValue((uint)id, out Display? display))
                return display;
            else
                return null;
        }

        public unsafe Display? GetNearestDisplayForPoint(Rect rect)
        {
            SDL_DisplayID id = SDL3.SDL_GetDisplayForRect((SDL_Rect*)&rect);

            if (_displays.TryGetValue((uint)id, out Display? display))
                return display;
            else
                return null;
        }

        public Window? PrimaryWindow { get => _primaryWindow; set => _primaryWindow = value; }

        public Dictionary<uint, Window> Windows => _windows;
        public Dictionary<uint, Display> Displays => _displays;

        public event Action<Window>? WindowDestroyed;

        private static WindowManager? s_instance;
        public static WindowManager Instance => s_instance!;
    }
}
