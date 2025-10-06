using Editor.LegacyGui.Elements;
using Primary.Common;
using Primary.Polling;
using Primary.Rendering;
using SDL;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.LegacyGui.Managers
{
    internal sealed class GuiEventManager : IDisposable, IEventHandler
    {
        private EditorGuiManager _guiManager;

        private List<ViewerCallback> _viewers;
        private ViewerCallback? _exclusiveViewer;
        private Window? _exclusiveViewerWindow;

        private Dictionary<Window, WindowTrackingData> _mouseTrackers;

        private Window? _exclusiveWindow;
        private Vector2 _exclusiveWindowStartPos;

        internal GuiEventManager(EditorGuiManager guiManager)
        {
            _guiManager = guiManager;

            _viewers = new List<ViewerCallback>();
            _exclusiveViewer = null;
            _exclusiveViewerWindow = null;

            _mouseTrackers = new Dictionary<Window, WindowTrackingData>();
            _exclusiveWindow = null;

            Editor.GlobalSingleton.EventManager.AddHandler(this);
        }

        public void Dispose()
        {
            Editor.GlobalSingleton.EventManager.RemoveHandler(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddViewer(ViewerCallback callback)
        {
            if (!_viewers.Contains(callback))
                _viewers.Add(callback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveViewer(ViewerCallback callback)
        {
            _viewers.Remove(callback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetExclusiveViewer(ViewerCallback? viewer, Window? window = null) { _exclusiveViewer = viewer; _exclusiveViewerWindow = window; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetExclusiveWindow(Window? window) { _exclusiveWindow = window; _exclusiveWindowStartPos = window?.Position ?? Vector2.Zero; }

        internal void PumpFrame()
        {
            if (_exclusiveWindow != null)
            {
                ref WindowTrackingData trackingData = ref CollectionsMarshal.GetValueRefOrAddDefault(_mouseTrackers, _exclusiveWindow, out bool exists);
                if (!exists)
                {
                    SetupTrackingDataFromMouseState(_exclusiveWindow, ref trackingData, false);
                    return;
                }

                SetupTrackingDataFromMouseState(_exclusiveWindow, ref trackingData, true);
            }
        }

        private void SetupTrackingDataFromMouseState(Window window, ref WindowTrackingData trackingData, bool sendEvents = false)
        {
            Vector2 mousePos;
            SDL_MouseButtonFlags flags;

            unsafe
            {
                flags = SDL3.SDL_GetGlobalMouseState(&mousePos.X, &mousePos.Y);
            }

            if (trackingData.Mouse != mousePos)
            {
                if (_exclusiveWindow == window)
                {
                    mousePos -= _exclusiveWindowStartPos;
                }
                else
                {
                    int x, y;
                    unsafe
                    {
                        SDL3.SDL_GetWindowPosition((SDL_Window*)window.InternalWindowInterop, &x, &y);
                    }
                    mousePos -= new Vector2(x, y);
                }

                UIEvent @event = new UIEvent
                {
                    Type = UIEventType.MouseMotion,
                    Hit = mousePos,
                    Mouse = new UIMouseEvent
                    {
                        MouseDelta = mousePos - trackingData.Mouse,
                    }
                };

                trackingData.Mouse = mousePos;
                if (sendEvents)
                    SendEvent(window.WindowId, @event);
            }

            UIMouseButton button = UIMouseButton.None;
            if (FlagUtility.HasFlag(flags, SDL_MouseButtonFlags.SDL_BUTTON_LMASK))
                button |= UIMouseButton.Left;
            if (FlagUtility.HasFlag(flags, SDL_MouseButtonFlags.SDL_BUTTON_MMASK))
                button |= UIMouseButton.Middle;
            if (FlagUtility.HasFlag(flags, SDL_MouseButtonFlags.SDL_BUTTON_RMASK))
                button |= UIMouseButton.Right;

            if (sendEvents && button != trackingData.Buttons)
            {
                if (FlagUtility.HasFlag(button, UIMouseButton.Left) != FlagUtility.HasFlag(trackingData.Buttons, UIMouseButton.Left))
                    SendMouseEvent(UIMouseButton.Left, FlagUtility.HasFlag(button, UIMouseButton.Left), trackingData.Mouse);
                if (FlagUtility.HasFlag(button, UIMouseButton.Middle) != FlagUtility.HasFlag(trackingData.Buttons, UIMouseButton.Middle))
                    SendMouseEvent(UIMouseButton.Middle, FlagUtility.HasFlag(button, UIMouseButton.Middle), trackingData.Mouse);
                if (FlagUtility.HasFlag(button, UIMouseButton.Right) != FlagUtility.HasFlag(trackingData.Buttons, UIMouseButton.Right))
                    SendMouseEvent(UIMouseButton.Right, FlagUtility.HasFlag(button, UIMouseButton.Right), trackingData.Mouse);

                void SendMouseEvent(UIMouseButton btn, bool down, Vector2 at)
                {
                    UIEvent @event = new UIEvent
                    {
                        Type = down ? UIEventType.MouseButtonDown : UIEventType.MouseButtonUp,
                        Hit = at,
                        Mouse = new UIMouseEvent
                        {
                            Button = btn
                        }
                    };

                    SendEvent(window.WindowId, @event);
                }
            }

            trackingData.Buttons = button;
        }

        public void Handle(ref readonly SDL_Event @event)
        {
            Window? window = Editor.GlobalSingleton.WindowManager.FindWindow(@event.window.windowID);
            if (window == null)
                return;

            ref WindowTrackingData trackingData = ref CollectionsMarshal.GetValueRefOrAddDefault(_mouseTrackers, window, out bool exists);
            if (!exists)
            {
                SetupTrackingDataFromMouseState(window, ref trackingData);
            }

            UIEvent uiEvent = new UIEvent
            {
                Type = (SDL_EventType)@event.type switch
                {
                    SDL_EventType.SDL_EVENT_MOUSE_MOTION => UIEventType.MouseMotion,
                    SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN => UIEventType.MouseButtonDown,
                    SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP => UIEventType.MouseButtonUp,
                    SDL_EventType.SDL_EVENT_MOUSE_WHEEL => UIEventType.MouseWheel,

                    SDL_EventType.SDL_EVENT_TEXT_INPUT => UIEventType.TextInput,

                    SDL_EventType.SDL_EVENT_KEY_DOWN => UIEventType.KeyDown,
                    SDL_EventType.SDL_EVENT_KEY_UP => UIEventType.KeyUp,

                    _ => UIEventType.None
                }
            };

            Vector2 hitPosition = Vector2.Zero;
            switch (uiEvent.Type)
            {
                case UIEventType.MouseMotion:
                    {
                        if (window == _exclusiveWindow)
                            return;

                        hitPosition = new Vector2(@event.motion.x, @event.motion.y);
                        uiEvent.Mouse = new UIMouseEvent
                        {
                            MouseDelta = trackingData.Mouse - hitPosition,
                        };

                        break;
                    }
                case UIEventType.MouseButtonDown:
                case UIEventType.MouseButtonUp:
                    {
                        if (window == _exclusiveWindow)
                            return;

                        if (@event.button.Button > SDLButton.SDL_BUTTON_RIGHT)
                            return;

                        hitPosition = new Vector2(@event.button.x, @event.button.y);
                        uiEvent.Mouse = new UIMouseEvent
                        {
                            Button = @event.button.Button switch
                            {
                                SDLButton.SDL_BUTTON_LEFT => UIMouseButton.Left,
                                SDLButton.SDL_BUTTON_MIDDLE => UIMouseButton.Middle,
                                SDLButton.SDL_BUTTON_RIGHT => UIMouseButton.Right,
                                _ => UIMouseButton.Left
                            }
                        };

                        break;
                    }
                case UIEventType.MouseWheel:
                    {
                        hitPosition = new Vector2(@event.wheel.mouse_x, @event.wheel.mouse_y);
                        uiEvent.Mouse = new UIMouseEvent
                        {
                            WheelDelta = new Vector2(@event.wheel.x, @event.wheel.y)
                        };

                        break;
                    }
                case UIEventType.TextInput:
                    {
                        hitPosition = Vector2.Zero;
                        uiEvent.TextInput = new UITextInputEvent
                        {
                            Letter = @event.text.GetText()![0]
                        };

                        break;
                    }
                case UIEventType.KeyDown:
                    break;
                case UIEventType.KeyUp:
                    break;
                default: return;
            }

            uiEvent.Hit = hitPosition;
            trackingData.Mouse = hitPosition;

            SendEvent(window.WindowId, uiEvent);
        }

        internal void SendEvent(uint windowId, UIEvent @event)
        {
            if (_exclusiveViewer != null)
            {
                if (_exclusiveViewerWindow != null && _exclusiveViewerWindow.WindowId != windowId)
                    return;

                _exclusiveViewer(in @event);
                return;
            }

            for (int i = 0; i < _viewers.Count; i++)
            {
                if (!_viewers[i](in @event))
                    return;
            }

            Vector2 mouse = @event.Hit;

            HashSet<DockSpace> dockSpaces = _guiManager.DockingManager.ActiveDockSpaces;
            foreach (DockSpace dockSpace in dockSpaces)
            {
                if (windowId == dockSpace.RootWindow!.WindowId)
                {
                    Vector2 minimum = dockSpace.LocalPosition;
                    Vector2 maximum = minimum + dockSpace.LocalSize;
                    bool isInsideBounds = mouse.X >= minimum.X && mouse.Y >= minimum.Y && mouse.X <= maximum.X && mouse.Y <= maximum.Y;

                    @event.Hit = GetLocalHitPosition(dockSpace, mouse);
                    if (isInsideBounds)
                    {
                        if (dockSpace.HandleEvent(in @event))
                            break;

                        Span<DockSpace.WindowTabData> activeWindows = dockSpace.Tabs;
                        for (int i = 0; i < activeWindows.Length; i++)
                        {
                            UIWindow window = activeWindows[i].Window!;

                            if (@event.Hit.X >= window.Position.Value.X && @event.Hit.Y >= window.Position.Value.Y &&
                                @event.Hit.X <= window.Position.Value.X + window.Size.Value.X && @event.Hit.Y <= window.Position.Value.Y + window.Size.Value.Y)
                            {
                                if (@event.Type == UIEventType.MouseButtonDown)
                                    _guiManager.SwitchWindowFocus(window);

                                if (window.HandleEvent(in @event))
                                {
                                    break;
                                }
                            }
                        }

                        break;
                    }
                }
            }

            static Vector2 GetLocalHitPosition(DockSpace dockSpace, Vector2 mouseHit)
            {
                DockSpace? parent = dockSpace;
                do
                {
                    mouseHit -= parent.LocalPosition;
                } while ((parent = parent.Parent) != null);

                return mouseHit;
            }
        }

        internal Vector2 GetMousePosition(Window window)
        {
            ref WindowTrackingData trackingData = ref CollectionsMarshal.GetValueRefOrAddDefault(_mouseTrackers, window, out bool exists);
            if (!exists)
            {
                SetupTrackingDataFromMouseState(window, ref trackingData);
            }

            return trackingData.Mouse;
        }

        internal delegate bool ViewerCallback(ref readonly UIEvent @event);

        private record struct WindowTrackingData(Vector2 Mouse, UIMouseButton Buttons);
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public record struct UIEvent
    {
        [FieldOffset(0)]
        public UIEventType Type;

        [FieldOffset(1)]
        public Vector2 Hit;

        [FieldOffset(9)]
        public UIMouseEvent Mouse;

        [FieldOffset(9)]
        public UITextInputEvent TextInput;

        [FieldOffset(9)]
        public UIKeyEvent Key;
    }

    public enum UIEventType : byte
    {
        None = 0,

        MouseMotion,
        MouseButtonDown,
        MouseButtonUp,
        MouseWheel,

        TextInput,

        KeyDown,
        KeyUp,
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public record struct UIMouseEvent
    {
        [FieldOffset(0)]
        public Vector2 MouseDelta;

        [FieldOffset(0)]
        public UIMouseButton Button;

        [FieldOffset(0)]
        public Vector2 WheelDelta;
    }

    public enum UIMouseButton : byte
    {
        None = 0,

        Left = 1 << 0,
        Middle = 1 << 1,
        Right = 1 << 2
    }

    public record struct UITextInputEvent
    {
        public char Letter;
    }

    public record struct UIKeyEvent
    {

    }
}
