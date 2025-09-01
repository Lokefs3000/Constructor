using Primary.Common;
using Primary.Polling;
using SDL;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SDL.SDL3;

namespace Primary.Rendering
{
    public class Window : IDisposable, IEventHandler
    {
        private string _windowTitle;
        private Vector2 _clientSize;
        private Vector2 _position;
        private bool _isFocused;
        private bool _isClosed;

        private SDL_PropertiesID _props;
        private nint _window;
        private SDL_WindowID _id;

        private HitTestDelegate? _currentHitTest;

        private GCHandle _handle;

        private bool _disposedValue;

        internal unsafe Window(string windowTitle, Vector2 clientSize, CreateWindowFlags flags)
        {
            _windowTitle = windowTitle;
            _clientSize = clientSize;
            _position = Vector2.Zero;
            _isFocused = false;
            _isClosed = false;

            _currentHitTest = null;

            _props = SDL_CreateProperties();

            SDL_SetStringProperty(_props, "SDL.window.create.title", windowTitle);
            SDL_SetNumberProperty(_props, SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, (long)clientSize.X);
            SDL_SetNumberProperty(_props, SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, (long)clientSize.Y);

            if (FlagUtility.HasFlag(flags, CreateWindowFlags.Borderless))
                SDL_SetBooleanProperty(_props, SDL_PROP_WINDOW_CREATE_BORDERLESS_BOOLEAN, true);
            if (FlagUtility.HasFlag(flags, CreateWindowFlags.Resizable))
                SDL_SetBooleanProperty(_props, SDL_PROP_WINDOW_CREATE_RESIZABLE_BOOLEAN, true);

            _window = (nint)SDL_CreateWindowWithProperties(_props);
            _id = SDL_GetWindowID((SDL_Window*)_window);

            SDL_DestroyProperties(_props);
            _props = SDL_GetWindowProperties((SDL_Window*)_window);

            int x, y;
            SDL_GetWindowPosition((SDL_Window*)_window, &x, &y);
            _position = new Vector2(x, y);

            _isFocused = FlagUtility.HasFlag(SDL_GetWindowFlags((SDL_Window*)_window), SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS);

            Engine.GlobalSingleton.EventManager.AddHandler(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ProcessEvent(SDL_Event @event) => Handle(ref @event);

        public void Handle(ref readonly SDL_Event @event)
        {
            if (@event.window.windowID == _id)
            {
                //TODO: convert to switch statement instead
                if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_MOVED)
                {
                    _position = new Vector2(@event.window.data1, @event.window.data2);
                    WindowMoved?.Invoke(_position);
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_RESIZED)
                {
                    _clientSize = new Vector2(@event.window.data1, @event.window.data2);
                    WindowResized?.Invoke(_clientSize);
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED)
                {
                    _isFocused = true;
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST)
                {
                    _isFocused = false;
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
                {
                    _isClosed = true;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_handle.IsAllocated)
                    _handle.Free();
                unsafe
                {
                    SDL_DestroyWindow((SDL_Window*)_window);
                }

                Engine.GlobalSingleton.EventManager.RemoveHandler(this);

                _disposedValue = true;
            }
        }

        ~Window()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private unsafe void ChangeHitTest(HitTestDelegate? @delegate)
        {
            if (_currentHitTest == @delegate)
                return;

            if (@delegate == null)
            {
                SDL_SetWindowHitTest((SDL_Window*)_window, null, nint.Zero);
                _currentHitTest = null;

                if (_handle.IsAllocated)
                    _handle.Free();

                return;
            }

            if (_currentHitTest == null)
            {
                if (!_handle.IsAllocated)
                    _handle = GCHandle.Alloc(this, GCHandleType.Weak);

                if (!SDL_SetWindowHitTest((SDL_Window*)_window, &HitTestWrapper, GCHandle.ToIntPtr(_handle)))
                    return;
            }

            _currentHitTest = @delegate;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe SDL_HitTestResult HitTestWrapper(SDL_Window* win, SDL_Point* area, nint data)
        {
            GCHandle handle = GCHandle.FromIntPtr(data);
            Window window = (Window)handle.Target!;

            if (window._currentHitTest != null)
            {
                return (SDL_HitTestResult)window._currentHitTest(window, new Vector2(area->x, area->y));
            }

            SDL_SetWindowHitTest((SDL_Window*)window._window, null, nint.Zero);
            if (window._handle.IsAllocated)
                window._handle.Free();

            return SDL_HitTestResult.SDL_HITTEST_NORMAL;
        }

        public unsafe bool TakeFocus() => SDL_RaiseWindow((SDL_Window*)_window);

        internal SDL_WindowID ID => _id;
        public uint WindowId => (uint)_id;

        public unsafe string WindowTitle { get => _windowTitle; set { if (SDL_SetWindowTitle((SDL_Window*)_window, value)) _windowTitle = value; } }
        public unsafe Vector2 ClientSize
        {
            get => _clientSize;
            set
            {
                if (SDL_SetWindowSize((SDL_Window*)_window, (int)value.X, (int)value.Y))
                {
                    _clientSize = value;
                    //WindowResized?.Invoke(_clientSize);
                }
            }
        }
        public unsafe Vector2 Position
        {
            get => _position;
            set
            {
                if (SDL_SetWindowPosition((SDL_Window*)_window, (int)value.X, (int)value.Y))
                {
                    _position = value;
                    //WindowMoved?.Invoke(_position);
                }
            }
        }

        public bool IsFocused => _isFocused;
        public bool IsClosed { get => _isClosed; set => _isClosed = value; }

        public HitTestDelegate? HitTest
        {
            get => _currentHitTest;
            set => ChangeHitTest(value);
        }

        public nint NativeWindowHandle => SDL_GetPointerProperty(_props, SDL_PROP_WINDOW_WIN32_HWND_POINTER, nint.Zero);
        public nint InternalWindowInterop => _window;

        public event Action<Window>? WindowClosed;
        public event Action<Vector2>? WindowResized;
        public event Action<Vector2>? WindowMoved;


        public delegate HitTestResult HitTestDelegate(Window window, Vector2 area);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate SDL_HitTestResult HitTestWrapperDelegate(SDL_Window win, System.Drawing.Point* area, nint data);
    }

    [Flags]
    public enum CreateWindowFlags : byte
    {
        None = 0,
        Borderless = 1 << 0,
        Resizable = 1 << 1,
    }

    public enum HitTestResult : byte
    {
        Normal = 0,
        Draggable = 1,
        ResizeTopLeft = 2,
        ResizeTop = 3,
        ResizeTopRight = 4,
        ResizeRight = 5,
        ResizeBottomRight = 6,
        ResizeBottom = 7,
        ResizeBottomLeft = 8,
        ResizeLeft = 9,
    }
}
