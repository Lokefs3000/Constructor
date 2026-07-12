using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Polling;
using Primary.Threading;
using SDL;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static SDL.SDL3;

namespace Primary.Windowing
{
    public unsafe sealed class Window : IDisposable, IEventHandler
    {
        private string _windowTitle;
        private Int2 _clientSize;
        private Int2 _position;
        private bool _isFocused;
        private bool _isClosed;
        private bool _isShown;
        private bool _isTransparent;

        private Display _display;

        private SDL_PropertiesID _props;
        private SDL_Window* _window;
        private SDL_WindowID _id;

        private HitTestDelegate? _currentHitTest;
        private GCHandle _hitProcGCHandle;

        private Window? _parentWindow;
        private List<Window>? _ownedWindows;

        private bool _isCurrentlyModal;

        private bool _disposedValue;

        internal unsafe Window(string windowTitle, Int2 clientSize, CreateWindowFlags flags)
        {
            _windowTitle = windowTitle;
            _clientSize = clientSize;
            _position = Int2.Zero;
            _isFocused = false;
            _isClosed = false;
            _isTransparent = Flags.HasFlag(flags, CreateWindowFlags.Transparent);

            _currentHitTest = null;
            _hitProcGCHandle = default;

            _parentWindow = null;
            _ownedWindows = null;
            _isCurrentlyModal = false;

            _props = SDL_CreateProperties();

            SDL_SetStringProperty(_props, "SDL.window.create.title", windowTitle);
            SDL_SetNumberProperty(_props, SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, (long)clientSize.X);
            SDL_SetNumberProperty(_props, SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, (long)clientSize.Y);

            if (Flags.HasFlag(flags, CreateWindowFlags.Borderless))
                SDL_SetBooleanProperty(_props, SDL_PROP_WINDOW_CREATE_BORDERLESS_BOOLEAN, true);
            if (Flags.HasFlag(flags, CreateWindowFlags.Resizable))
                SDL_SetBooleanProperty(_props, SDL_PROP_WINDOW_CREATE_RESIZABLE_BOOLEAN, true);
            if (Flags.HasFlag(flags, CreateWindowFlags.Hidden))
                SDL_SetBooleanProperty(_props, SDL_PROP_WINDOW_CREATE_HIDDEN_BOOLEAN, true);
            if (Flags.HasFlag(flags, CreateWindowFlags.AlwaysOnTop))
                SDL_SetBooleanProperty(_props, SDL_PROP_WINDOW_CREATE_ALWAYS_ON_TOP_BOOLEAN, true);
            if (Flags.HasFlag(flags, CreateWindowFlags.Transparent))
                SDL_SetBooleanProperty(_props, SDL_PROP_WINDOW_CREATE_TRANSPARENT_BOOLEAN, true);

            _window = SDL_CreateWindowWithProperties(_props);
            _id = SDL_GetWindowID(_window);

            SDL_DestroyProperties(_props);
            _props = SDL_GetWindowProperties(_window);

            fixed (Int2* pos = &_position)
            {
                SDL_GetWindowPosition(_window, &pos->X, &pos->Y);
            }

            if (OperatingSystem.IsWindows())
            {
#pragma warning disable CA1416 // Validate platform compatibility

                // Annoying workaround for SDL because doesn't support the extended window style
                if (Flags.HasFlag(flags, CreateWindowFlags.Transparent))
                {
                    nint oldWindowStyle = Windows.GetWindowLongPtr((HWND)NativeWindowHandle, GWL.GWL_EXSTYLE);
                    Windows.SetWindowLongPtr((HWND)NativeWindowHandle, GWL.GWL_EXSTYLE, oldWindowStyle | WS.WS_EX_NOREDIRECTIONBITMAP);

                    // Ensure no data is cached to allow for an immediate window update
                    SDL_SetWindowPosition(_window, _position.X, _position.Y);

#pragma warning restore CA1416 // Validate platform compatibility
                }
            }

            _display = Engine.GlobalSingleton.WindowManager.Displays[(uint)SDL_GetDisplayForWindow(_window)];

            _isFocused = Flags.HasFlag(SDL_GetWindowFlags(_window), SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS);
            _isShown = !Flags.HasFlag(SDL_GetWindowFlags(_window), SDL_WindowFlags.SDL_WINDOW_HIDDEN);

            Engine.GlobalSingleton.EventManager.AddHandler(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                WindowManager.Instance.DestroyWindow(this);
                OnDestroy?.Invoke(this);

                ThreadHelper.ExecuteOnMainThread(() =>
                {
                    if (_hitProcGCHandle.IsAllocated)
                        _hitProcGCHandle.Free();

                    if (_window != null)
                        SDL_DestroyWindow(_window);

                    _hitProcGCHandle = default;
                    _window = null;
                });

                Engine.GlobalSingleton.EventManager.RemoveHandler(this);

                if (_parentWindow != null)
                    SetWindowParent(null);
                if (_ownedWindows != null)
                {
                    foreach (Window window in _ownedWindows)
                    {
                        window.Dispose();
                    }

                    _ownedWindows = null;
                }

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

        public void Handle(ref readonly SDL_Event @event)
        {
            if (@event.window.windowID == _id)
            {
                // TODO: convert to switch statement instead
                // UPDATE: yandere-dev level coding and im still too lazy to refactor -_-

                if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_MOVED)
                {
                    _position = new Int2(@event.window.data1, @event.window.data2);
                    WindowMoved?.Invoke(_position);
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_RESIZED)
                {
                    _clientSize = new Int2(@event.window.data1, @event.window.data2);
                    WindowResized?.Invoke(_clientSize);
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED)
                {
                    _isFocused = true;
                    OnFocusGained?.Invoke(this);
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST)
                {
                    _isFocused = false;
                    OnFocusLost?.Invoke(this);
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
                {
                    _isClosed = true;
                }
                else if (@event.Type == SDL_EventType.SDL_EVENT_WINDOW_DISPLAY_CHANGED)
                {
                    _display = Engine.GlobalSingleton.WindowManager.Displays[(uint)@event.window.data1];
                }
                else if (@event.window.type == SDL_EventType.SDL_EVENT_WINDOW_SHOWN)
                {
                    _isShown = true;
                    OnVisiblityChanged?.Invoke(_isShown);
                }
                else if (@event.Type == SDL_EventType.SDL_EVENT_WINDOW_HIDDEN)
                {
                    _isShown = false;
                    OnVisiblityChanged?.Invoke(_isShown);
                }
                else if (@event.Type == SDL_EventType.SDL_EVENT_WINDOW_DESTROYED)
                {
                    if (!_disposedValue)
                        EngLog.Core.Error("Window destroyed event recieved without previous dispose call");
                }
            }
        }

        private void SetWindowParent(Window? newParentWindow)
        {
            if (_parentWindow == newParentWindow)
                return;

            if (_isCurrentlyModal)
                SDL_SetWindowModal(_window, false);

            if (_parentWindow != null)
            {
                _parentWindow._ownedWindows?.Remove(this);
            }

            if (newParentWindow != null)
            {
                (newParentWindow._ownedWindows ??= new List<Window>()).Add(this);

                SDL_SetWindowParent(_window, newParentWindow._window);
                if (_isCurrentlyModal)
                    _isCurrentlyModal = SDL_SetWindowModal(_window, true);
            }
            else
            {
                SDL_SetWindowParent(_window, null);
            }

            _parentWindow = newParentWindow;
        }

        public unsafe void StartTextInput() => SDL_StartTextInput(_window);

        private unsafe void ChangeHitTest(HitTestDelegate? @delegate)
        {
            if (_currentHitTest == @delegate)
                return;

            if (@delegate == null)
            {
                SDL_SetWindowHitTest(_window, null, nint.Zero);
                _currentHitTest = null;

                if (_hitProcGCHandle.IsAllocated)
                    _hitProcGCHandle.Free();

                return;
            }

            if (_currentHitTest == null)
            {
                if (!_hitProcGCHandle.IsAllocated)
                    _hitProcGCHandle = GCHandle.Alloc(this, GCHandleType.Weak);

                if (!SDL_SetWindowHitTest(_window, &HitTestWrapper, GCHandle.ToIntPtr(_hitProcGCHandle)))
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
                return (SDL_HitTestResult)window._currentHitTest(window, new Int2(area->x, area->y));
            }

            SDL_SetWindowHitTest(window._window, null, nint.Zero);
            if (window._hitProcGCHandle.IsAllocated)
                window._hitProcGCHandle.Free();

            return SDL_HitTestResult.SDL_HITTEST_NORMAL;
        }

        public unsafe bool TakeFocus() => SDL_RaiseWindow(_window);

        public void Show() => IsShown = true;
        public void Hide() => IsShown = false;

        public override string ToString()
        {
            return $"Window{{ {WindowTitle} }}";
        }

        public uint WindowId => (uint)_id;

        public unsafe string WindowTitle { get => _windowTitle; set { if (SDL_SetWindowTitle(_window, value)) _windowTitle = value; } }
        public unsafe Int2 ClientSize
        {
            get => _clientSize;
            set
            {
                if (SDL_SetWindowSize(_window, value.X, value.Y))
                {
                    SDL_SyncWindow(_window);
                    fixed (Int2* ptr = &_clientSize)
                        SDL_GetWindowSizeInPixels(_window, &ptr->X, &ptr->Y);
                    //WindowResized?.Invoke(_clientSize);
                }
                else
                    EngLog.Render.Error("Failed to set window client size\n    {reason}", SDL_GetError());
            }
        }
        public unsafe Int2 Position
        {
            get => _position;
            set
            {
                if (SDL_SetWindowPosition(_window, value.X, value.Y))
                {
                    SDL_SyncWindow(_window);
                    fixed (Int2* ptr = &_clientSize)
                        SDL_GetWindowPosition(_window, &ptr->X, &ptr->Y);
                    //WindowMoved?.Invoke(_clientSize);
                }
                else
                    EngLog.Render.Error("Failed to set window position\n    {reason}", SDL_GetError());
            }
        }

        public Rect ClientRect => new Rect(_position, _clientSize);

        public bool IsFocused => _isFocused;
        public bool IsClosed { get => _isClosed; set => _isClosed = value; }
        public unsafe bool IsShown
        {
            get => _isShown;
            set
            {
                if (value != _isShown)
                {
                    _isShown = value;
                    if (value)
                        SDL_ShowWindow(_window);
                    else
                        SDL_HideWindow(_window);
                }
            }
        }

        public bool IsTransparent => _isTransparent;

        public bool IsPrimary => WindowManager.Instance.PrimaryWindow == this;

        public Display Display => _display;

        public HitTestDelegate? HitTest
        {
            get => _currentHitTest;
            set => ChangeHitTest(value);
        }

        public Window? Parent { get => _parentWindow; set => SetWindowParent(value); }
        public ROList<Window> Children => _ownedWindows ?? ROList<Window>.Empty;

        public bool IsModal
        {
            get => _isCurrentlyModal;
            set
            {
                if (_parentWindow != null)
                    _isCurrentlyModal = SDL_SetWindowModal(_window, value) && _isCurrentlyModal;
                else
                    _isCurrentlyModal = value;
            }
        }

        public bool IsDestroyed => _disposedValue;

        public nint NativeWindowHandle => SDL_GetPointerProperty(_props, SDL_PROP_WINDOW_WIN32_HWND_POINTER, nint.Zero);
        public nint InternalWindowInterop => (nint)_window;

        public event Action<Window>? WindowClosed;
        public event Action<Window>? OnDestroy;

        public event Action<Int2>? WindowResized;
        public event Action<Int2>? WindowMoved;

        public event Action<bool>? OnVisiblityChanged;

        public event Action<Window>? OnFocusGained;
        public event Action<Window>? OnFocusLost;

        public delegate HitTestResult HitTestDelegate(Window window, Int2 area);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate SDL_HitTestResult HitTestWrapperDelegate(SDL_Window win, System.Drawing.Point* area, nint data);
    }

    [Flags]
    public enum CreateWindowFlags : byte
    {
        None = 0,
        Borderless = 1 << 0,
        Resizable = 1 << 1,
        Hidden = 1 << 2,
        AlwaysOnTop = 1 << 3,
        Transparent = 1 << 4
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
