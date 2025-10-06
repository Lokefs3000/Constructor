using Editor.Gui.Events;
using Primary;
using Primary.Common;
using Primary.Polling;
using Primary.Rendering;
using SDL;
using Serilog;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static SDL.SDL3;

namespace Editor.Gui
{
    public sealed class UIEventManager : IDisposable, IEventHandler
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private Queue<QueuedEvent> _queuedEvents;

        private uint _currentActiveWindowId;
        private Dictionary<Window, WindowEventData> _trackedWindows;

        private WeakReference _focusedDockingContainer;

        private UICursorType _currentCursor;
        private nint[] _cursors;

        private Vector2 _globalMousePosition;
        private List<DockingContainer> _globalEventListeners;

        private bool _disposedValue;

        internal UIEventManager()
        {
            s_instance.Target = this;

            _queuedEvents = new Queue<QueuedEvent>();

            _currentActiveWindowId = uint.MaxValue;
            _trackedWindows = new Dictionary<Window, WindowEventData>();

            _focusedDockingContainer = new WeakReference(null);

            _currentCursor = UICursorType.Default;
            _cursors = new nint[Enum.GetValues<UICursorType>().Length];

            unsafe
            {
                for (int i = 0; i < _cursors.Length; i++)
                {
                    _cursors[i] = (nint)SDL_CreateSystemCursor((SDL_SystemCursor)i);

                    if (_cursors[i] == nint.Zero)
                    {
                        Log.Error("Cursor err: {err}", SDL_GetError());
                    }
                }
            }

            _globalMousePosition = Vector2.Zero;
            _globalEventListeners = new List<DockingContainer>();

            Engine.GlobalSingleton.EventManager.AddHandler(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    s_instance.Target = null;

                    foreach (var kvp in _trackedWindows)
                        kvp.Value.Window.WindowClosed -= WindowClosedCallback;
                    _trackedWindows.Clear();
                }

                unsafe
                {
                    foreach (nint cursor in _cursors)
                        SDL_DestroyCursor((SDL_Cursor*)cursor);
                }
                _cursors = Array.Empty<nint>();

                Engine.GlobalSingleton.EventManager.RemoveHandler(this);

                _disposedValue = true;
            }
        }

        ~UIEventManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private HashSet<Window> _updatedWindows = new HashSet<Window>();
        internal void Update()
        {
            unsafe
            {
                SDL_SetCursor((SDL_Cursor*)_cursors[(int)_currentCursor]);
            }
            //_currentCursor = UICursorType.Default;

            _globalMousePosition = Vector2.Zero;
            UIMouseButton buttons = UIMouseButton.None;

            unsafe
            {
                fixed (Vector2* mouse = &_globalMousePosition)
                {
                    float* val = (float*)mouse;
                    SDL_MouseButtonFlags flags = SDL_GetGlobalMouseState(&val[0], &val[1]);

                    if (FlagUtility.HasFlag(flags, SDL_MouseButtonFlags.SDL_BUTTON_LMASK))
                        buttons |= UIMouseButton.Left;
                    if (FlagUtility.HasFlag(flags, SDL_MouseButtonFlags.SDL_BUTTON_MMASK))
                        buttons |= UIMouseButton.Middle;
                    if (FlagUtility.HasFlag(flags, SDL_MouseButtonFlags.SDL_BUTTON_RMASK))
                        buttons |= UIMouseButton.Right;
                }
            }

            _updatedWindows.Clear();
            for (int i = 0; i < _globalEventListeners.Count; i++)
            {
                DockingContainer container = _globalEventListeners[i];
                if (container.Window != null && container.Window.IsFocused)
                {
                    ref WindowEventData eventData = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedWindows, container.Window, out bool exists);
                    if (!exists)
                    {
                        eventData.Window = container.Window;
                        eventData.MousePosition = _globalMousePosition - container.Window.Position;
                        eventData.MouseButtons = buttons;

                        eventData.Window.WindowClosed += WindowClosedCallback;
                    }

                    Vector2 localPosition = _globalMousePosition - container.Window.Position;

                    if (eventData.MouseButtons != buttons)
                    {
                        UIMouseButton button = UIMouseButton.Left;
                        for (int j = 0; j < 3; j++)
                        {
                            button = (UIMouseButton)(1 << i);

                            UIMouseButton old = eventData.MouseButtons & button;
                            UIMouseButton @new = buttons & button;

                            if (old != @new)
                            {
                                _queuedEvents.Enqueue(new QueuedEvent(new UIEvent
                                {
                                    Type = @new > 0 ? UIEventType.MouseButtonDown : UIEventType.MouseButtonUp,
                                    WindowId = container.Window.WindowId,
                                    MouseHit = localPosition,
                                    Mouse = new UIEvent.MouseData(localPosition - eventData.MousePosition, button)
                                }, container));
                            }
                        }
                    }

                    if (localPosition != eventData.Window.Position)
                    {
                        _queuedEvents.Enqueue(new QueuedEvent(new UIEvent
                        {
                            Type = UIEventType.MouseMotion,
                            WindowId = container.Window.WindowId,
                            MouseHit = localPosition,
                            Mouse = new UIEvent.MouseData(eventData.MousePosition - localPosition, buttons)
                        }, container));
                    }
                }
            }

            EditorGuiManager guiManager = EditorGuiManager.Instance;
            while (_queuedEvents.TryDequeue(out QueuedEvent queued))
            {
                UIEvent @event = queued.EventData;

                _currentActiveWindowId = @event.WindowId;

                if (@event.Type <= UIEventType.MouseWheel)
                    SetMousePositionFor(@event.WindowId, @event.MouseHit);

                if (@event.Type == UIEventType.MouseMotion)
                    SetMouseButtonsFor(@event.WindowId, @event.Mouse.Button);
                else if (@event.Type == UIEventType.MouseButtonDown)
                    SetMouseButtonsFor(@event.WindowId, GetMouseButtonsFor(@event.WindowId) | @event.Mouse.Button);
                else if (@event.Type == UIEventType.MouseButtonUp)
                    SetMouseButtonsFor(@event.WindowId, GetMouseButtonsFor(@event.WindowId) & ~@event.Mouse.Button);

                if (queued.Container != null)
                {
                    Boundaries boundaries = GetAbsoluteElementSpace(queued.Container);
                    @event.MouseHit -= boundaries.Minimum;

                    if (!queued.Container.ProcessEvent(ref @event))
                    {

                    }

                    continue;
                }

                bool hasFoundValidSpace = false;
                foreach (DockingContainer container in guiManager.ActiveContainers)
                {
                    if (container.Window != null && container.Window.WindowId == @event.WindowId && !_globalEventListeners.Contains(container))
                    {
                        Boundaries boundaries = GetAbsoluteElementSpace(container);
                        if (boundaries.IsWithin(@event.MouseHit))
                        {
                            hasFoundValidSpace = true;

                            if (_focusedDockingContainer.Target != container)
                            {
                                if (_focusedDockingContainer.Target != null)
                                {
                                    _queuedEvents.Enqueue(new QueuedEvent(new UIEvent
                                    {
                                        Type = UIEventType.MouseLeave,
                                        WindowId = container.Window.WindowId,
                                        MouseHit = @event.MouseHit
                                    }, (DockingContainer)_focusedDockingContainer.Target));
                                }

                                _queuedEvents.Enqueue(new QueuedEvent(new UIEvent
                                {
                                    Type = UIEventType.MouseEnter,
                                    WindowId = container.Window.WindowId,
                                    MouseHit = @event.MouseHit
                                }, container));

                                _focusedDockingContainer.Target = container;
                            }

                            Vector2 localMouseHit = @event.MouseHit - boundaries.Minimum;
                            if (!container.ProcessEvent(ref @event))
                            {

                            }

                            break;
                        }
                    }
                }

                if (hasFoundValidSpace)
                {

                }
            }
        }

        internal static void SetGlobalEvents(DockingContainer container, bool enabled)
        {
            UIEventManager @this = NullableUtility.ThrowIfNull((UIEventManager?)s_instance.Target);
            if (enabled)
            {
                if (!@this._globalEventListeners.Contains(container))
                {
                    @this._globalEventListeners.Add(container);

                    if (@this._focusedDockingContainer.Target == container)
                        @this._focusedDockingContainer.Target = null;
                }
            }
            else
            {
                @this._globalEventListeners.Remove(container);

                if (container.Window != null)
                {
                    Boundaries boundaries = GetAbsoluteElementSpace(container);
                    Vector2 mouse = @this.GetMousePositionFor(container.Window.WindowId);
                    if (!boundaries.IsWithin(mouse))
                    {
                        @this._queuedEvents.Enqueue(new QueuedEvent(new UIEvent
                        {
                            Type = UIEventType.MouseLeave,
                            WindowId = container.Window.WindowId,
                            MouseHit = mouse,
                        }, container));
                    }
                }
            }
        }

        public void Handle(ref readonly SDL_Event @event)
        {
            UIEventType eventType = (SDL_EventType)@event.type switch
            {
                SDL_EventType.SDL_EVENT_MOUSE_MOTION => UIEventType.MouseMotion,
                SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN => UIEventType.MouseButtonDown,
                SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP => UIEventType.MouseButtonUp,
                SDL_EventType.SDL_EVENT_MOUSE_WHEEL => UIEventType.MouseWheel,
                _ => (UIEventType)byte.MaxValue
            };

            if (eventType == (UIEventType)byte.MaxValue)
                return;

            UIEvent uiEvent = new UIEvent
            {
                Type = eventType,
                WindowId = (uint)@event.window.windowID
            };

            switch (eventType)
            {
                case UIEventType.MouseMotion:
                    {
                        uiEvent.MouseHit = new Vector2(@event.motion.x, @event.motion.y);
                        uiEvent.Mouse = new UIEvent.MouseData(uiEvent.MouseHit - GetMousePositionFor((uint)@event.window.windowID), GetMouseButtonsFor((uint)@event.window.windowID));
                        break;
                    }
                case UIEventType.MouseButtonUp:
                case UIEventType.MouseButtonDown:
                    {
                        uiEvent.MouseHit = new Vector2(@event.button.x, @event.button.y);

                        UIMouseButton button = @event.button.Button switch
                        {
                            SDLButton.SDL_BUTTON_LEFT => UIMouseButton.Left,
                            SDLButton.SDL_BUTTON_MIDDLE => UIMouseButton.Middle,
                            SDLButton.SDL_BUTTON_RIGHT => UIMouseButton.Right,
                            _ => UIMouseButton.None
                        };

                        uiEvent.Mouse = new UIEvent.MouseData(uiEvent.MouseHit - GetMousePositionFor((uint)@event.window.windowID), button);
                        break;
                    }
                case UIEventType.MouseWheel:
                    {
                        uiEvent.MouseHit = new Vector2(@event.wheel.mouse_x, @event.wheel.mouse_y);
                        uiEvent.Mouse = new UIEvent.MouseData(uiEvent.MouseHit - GetMousePositionFor((uint)@event.window.windowID), GetMouseButtonsFor((uint)@event.window.windowID));
                        break;
                    }
            }

            _queuedEvents.Enqueue(new QueuedEvent(uiEvent, null));
        }

        private void SetMousePositionFor(uint windowId, Vector2 mousePosition)
        {
            Window? window = WindowManager.Instance.FindWindow((SDL_WindowID)windowId);
            if (window != null)
            {
                ref WindowEventData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedWindows, window, out bool exists);
                if (!exists)
                {
                    data.Window = window;
                    data.MousePosition = Vector2.Zero;
                    data.MouseButtons = UIMouseButton.None;

                    window.WindowClosed += WindowClosedCallback;
                }

                data.MousePosition = mousePosition;
            }
        }

        private void SetMouseButtonsFor(uint windowId, UIMouseButton mouseButtons)
        {
            Window? window = WindowManager.Instance.FindWindow((SDL_WindowID)windowId);
            if (window != null)
            {
                ref WindowEventData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_trackedWindows, window, out bool exists);
                if (!exists)
                {
                    data.Window = window;
                    data.MousePosition = Vector2.Zero;
                    data.MouseButtons = UIMouseButton.None;

                    window.WindowClosed += WindowClosedCallback;
                }

                data.MouseButtons = mouseButtons;
            }
        }

        private Vector2 GetMousePositionFor(uint windowId)
        {
            Window? window = WindowManager.Instance.FindWindow((SDL_WindowID)windowId);
            if (window != null)
            {
                ref WindowEventData data = ref CollectionsMarshal.GetValueRefOrNullRef(_trackedWindows, window);
                if (!Unsafe.IsNullRef(ref data))
                {
                    return data.MousePosition;
                }
            }

            return Vector2.Zero;
        }

        private UIMouseButton GetMouseButtonsFor(uint windowId)
        {
            Window? window = WindowManager.Instance.FindWindow((SDL_WindowID)windowId);
            if (window != null)
            {
                ref WindowEventData data = ref CollectionsMarshal.GetValueRefOrNullRef(_trackedWindows, window);
                if (!Unsafe.IsNullRef(ref data))
                {
                    return data.MouseButtons;
                }
            }

            return UIMouseButton.None;
        }

        private Vector2 GetGlobalMousePosition(uint windowId)
        {
            Window? window = WindowManager.Instance.FindWindow((SDL_WindowID)windowId);
            return _globalMousePosition - window?.Position ?? Vector2.Zero;
        }

        private void WindowClosedCallback(Window window)
        {
            _trackedWindows.Remove(window);
        }

        public static void SetCursor(UICursorType cursorType)
        {
            UIEventManager @this = NullableUtility.ThrowIfNull((UIEventManager?)s_instance.Target);
            @this._currentCursor = cursorType;
        }

        private static Boundaries GetAbsoluteElementSpace(DockingContainer container)
        {
            Boundaries boundaries = container.ElementSpace;

            DockingContainer? parent = null;
            while ((parent = parent?.OwningContainer) != null)
            {
                boundaries = Boundaries.Offset(boundaries, parent.Position);
            }

            return boundaries;
        }

        public static Vector2 MousePosition => Instance.GetMousePositionFor(Instance._currentActiveWindowId);
        public static UIMouseButton MouseButtons => Instance.GetMouseButtonsFor(Instance._currentActiveWindowId);

        public Vector2 GlobalMousePosition => GetGlobalMousePosition(_currentActiveWindowId);

        internal static UIEventManager Instance => NullableUtility.ThrowIfNull((UIEventManager?)s_instance.Target);

        private record struct WindowEventData(Window Window, Vector2 MousePosition, UIMouseButton MouseButtons);
        private record struct QueuedEvent(UIEvent EventData, DockingContainer? Container);
    }

    public enum UICursorType : byte
    {
        Default = 0,
        Text,
        Wait,
        Crosshair,
        Progress,
        NWSEResize,
        NESWResize,
        EWResize,
        NSResize,
        Move,
        NotAllowed,
        Pointer,
        NWResize,
        NResize,
        NEResize,
        EResize,
        SEResize,
        SResize,
        SWResize,
        WResize
    }
}
