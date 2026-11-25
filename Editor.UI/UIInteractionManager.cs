using CommunityToolkit.HighPerformance;
using Editor.UI.Elements;
using Editor.UI.Interaction;
using Primary;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Windowing;
using SDL;
using System.Diagnostics;
using System.Numerics;
using TerraFX.Interop.Windows;

namespace Editor.UI
{
    public sealed class UIInteractionManager : IDisposable
    {
        private readonly UIManager _manager;

        private Window? _currentWindow;

        private IInteractable? _interactableUnderMouse;
        private MouseHeldData[] _buttonHeldData;
        private MouseDragData[] _mouseDragData;

        private Vector2 _mousePosition;
        private Vector2 _mouseDelta;
        private Vector2 _mouseWheelDelta;

        private bool _disposedValue;

        internal UIInteractionManager(UIManager manager)
        {
            _manager = manager;

            _currentWindow = null;

            _interactableUnderMouse = null;
            _buttonHeldData = new MouseHeldData[3];
            _mouseDragData = new MouseDragData[3];

            _mousePosition = Vector2.Zero;
            _mouseDelta = Vector2.Zero;
            _mouseWheelDelta = Vector2.Zero;

            for (int i = 0; i < _mouseDragData.Length; i++)
            {
                _mouseDragData[i] = new MouseDragData(new EventDispatcher(this), false, Vector2.Zero);
            }

            Engine.GlobalSingleton.EventManager.EventRecieved += EventCallback;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Engine.GlobalSingleton.EventManager.EventRecieved -= EventCallback;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void RemoveReferencesFor(IInteractable interactable)
        {
            if (_interactableUnderMouse == interactable)
                ChangeMouseHover(null, null);

            for (int i = 0; i < _buttonHeldData.Length; i++)
            {
                ref MouseHeldData heldData = ref _buttonHeldData[i];
                if (heldData.Interactable == interactable)
                {
                    OnInteractableReleased(heldData.Interactable, (MouseButton)i);
                    Debug.Assert(heldData.Interactable == null && !heldData.IsHeld);
                }

                if (heldData.WithFocus == interactable)
                {
                    ChangeInteractableFocus(null, (MouseButton)i);
                    Debug.Assert(heldData.WithFocus == null);
                }
            }
        }

        internal void FireEventForInteractable(IInteractable element, UIEvent eventData)
        {
            element.Host?.InteractionManager?.HandleFiredEvent(element, ref eventData);
            element.HandleEvent(ref eventData);
        }

        internal void FireEventForInteractable(IInteractable element, UIEventType eventType, UIMouseEvent mouseEvent) => FireEventForInteractable(element, new UIEvent(eventType, this, mouseEvent));
        internal void FireEventForInteractable(IInteractable element, UIEventType eventType, UIDragEvent dragEvent) => FireEventForInteractable(element, new UIEvent(eventType, this, dragEvent));
        internal void FireEventForInteractable(IInteractable element, UIEventType eventType, UIKeyEvent keyEvent) => FireEventForInteractable(element, new UIEvent(eventType, this, keyEvent));

        private void ChangeMouseHover(IInteractable? newInteractable, Window? newWindow)
        {
            if (_interactableUnderMouse != newInteractable)
            {
                if (_interactableUnderMouse != null)
                {
                    Vector2 mousePosition = _mousePosition;
                    if (_interactableUnderMouse.Host != null)
                    {
                        mousePosition -= _interactableUnderMouse.Host.ContentMetrics.Position.AsVector2();
                    }

                    FireEventForInteractable(_interactableUnderMouse, UIEventType.MouseLeave, new UIMouseEvent
                    {
                        Position = mousePosition,
                        Delta = _mouseDelta
                    });
                }

                if (newInteractable != null)
                {
                    Vector2 mousePosition = _mousePosition;
                    if (newInteractable.Host != null)
                    {
                        mousePosition -= newInteractable.Host.ContentMetrics.Position.AsVector2();
                    }

                    FireEventForInteractable(newInteractable, UIEventType.MouseEnter, new UIMouseEvent
                    {
                        Position = mousePosition,
                        Delta = _mouseDelta
                    });
                }

                _interactableUnderMouse = newInteractable;
            }

            _currentWindow = newWindow;
        }

        private void OnInteractablePressed(IInteractable newInteractable, MouseButton button)
        {
            ref MouseHeldData heldData = ref _buttonHeldData[(int)button];
            if (heldData.Interactable != null)
            {
                Debug.Assert(heldData.IsHeld);
                OnInteractableReleased(newInteractable, button);
            }

            Debug.Assert(heldData.Interactable == null);
            Debug.Assert(!heldData.IsHeld);

            Vector2 mousePosition = _mousePosition;
            if (newInteractable.Host != null)
            {
                mousePosition -= newInteractable.Host.ContentMetrics.Position.AsVector2();
            }

            FireEventForInteractable(newInteractable, UIEventType.MouseDown, new UIMouseEvent { Position = mousePosition, Button = button });

            if (newInteractable is UIElement element)
            {
                element.Invoke_OnMouseDown(button);
            }

            heldData.Interactable = newInteractable;
            heldData.IsHeld = true;

            ref MouseDragData dragData = ref _mouseDragData[(int)button];
            if (!dragData.IsDragActive)
            {
                dragData.StartPosition = _mousePosition;
            }
        }

        private void OnInteractableReleased(IInteractable? interactable, MouseButton button)
        {
            ref MouseHeldData heldData = ref _buttonHeldData[(int)button];
            if (heldData.Interactable == null)
            {
                Debug.Assert(!heldData.IsHeld);
                return;
            }

            Debug.Assert(heldData.Interactable != null);
            Debug.Assert(heldData.IsHeld);

            Vector2 mousePosition = _mousePosition;
            if (heldData.Interactable.Host != null)
            {
                mousePosition -= heldData.Interactable.Host.ContentMetrics.Position.AsVector2();
            }

            FireEventForInteractable(heldData.Interactable, UIEventType.MouseUp, new UIMouseEvent { Position = mousePosition, Button = button });

            ref MouseDragData dragData = ref _mouseDragData[(int)button];
            if (dragData.IsDragActive)
            {
                dragData.Dispatcher.EndCurrentDrag(heldData.Interactable, button, _mousePosition);
                dragData.IsDragActive = false;
            }

            if (heldData.Interactable == interactable)
            {
                FireEventForInteractable(heldData.Interactable, UIEventType.MouseActivate, new UIMouseEvent { Position = mousePosition, Button = button });
            }

            if (interactable is UIElement element)
            {
                element.Invoke_OnMouseUp(button);

                if (heldData.Interactable == interactable)
                {
                    element.Invoke_OnMouseActivate(button);
                }
            }

            heldData.Interactable = null;
            heldData.IsHeld = false;
        }

        private void ChangeInteractableFocus(IInteractable? newInteractable, MouseButton button)
        {
            ref MouseHeldData heldData = ref _buttonHeldData[(int)button];

            if (heldData.WithFocus != null)
            {
                Vector2 mousePosition = _mousePosition;
                if (heldData.WithFocus.Host != null)
                {
                    mousePosition -= heldData.WithFocus.Host.ContentMetrics.Position.AsVector2();
                }

                FireEventForInteractable(heldData.WithFocus, UIEventType.MouseFocusLost, new UIMouseEvent
                {
                    Position = mousePosition,
                    Delta = _mouseDelta
                });

                if (heldData.WithFocus is UIElement element)
                {
                    element.Invoke_OnMouseFocusLost(button);
                }
            }

            heldData.WithFocus = newInteractable;
        }

        private void EventCallback(SDL_Event @event)
        {
            if (_manager.FindDockHostFromWindowId((uint)@event.window.windowID) is not IWindowHost host)
                return;

            switch (@event.Type)
            {
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    {
                        Vector2 position = new Vector2(@event.motion.x, @event.motion.y);
                        Vector2 delta = new Vector2(@event.motion.xrel, @event.motion.yrel);

                        _mousePosition = position;
                        _mouseDelta = delta;

                        IWindowHost? childHost = FindHostForEvent(host, position);
                        if (childHost != null)
                        {
                            IInteractable? interactable = FindTopNodeAtPosition(childHost, position);
                            ChangeMouseHover(interactable, host.HostWindow);

                            if (interactable != null)
                            {
                                Vector2 mousePosition = position;
                                if (interactable.Host != null)
                                {
                                    mousePosition -= interactable.Host.ContentMetrics.Position.AsVector2();
                                }

                                FireEventForInteractable(interactable, UIEventType.MouseMotion, new UIMouseEvent { Position = mousePosition, Delta = delta });

                                if (interactable is UIElement element)
                                {
                                    element.Invoke_OnMouseMove(mousePosition);
                                }
                            }

                            for (int i = 0; i < _mouseDragData.Length; i++)
                            {
                                ref MouseDragData dragData = ref _mouseDragData[i];
                                ref MouseHeldData heldData = ref _buttonHeldData[i];

                                if (dragData.IsDragActive)
                                {
                                    Debug.Assert(heldData.Interactable != null && heldData.IsHeld);

                                    dragData.Dispatcher.UpdateCurrentDrag(heldData.Interactable, position);
                                }
                                else if (heldData.IsHeld && Vector2.Distance(dragData.StartPosition, position) > 1.0f)
                                {
                                    Debug.Assert(heldData.Interactable != null);

                                    dragData.IsDragActive = true;
                                    dragData.Dispatcher.SetupForNewDrag(heldData.Interactable, (MouseButton)i, dragData.StartPosition, position);
                                }
                            }
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    {
                        Vector2 position = new Vector2(@event.wheel.mouse_x, @event.wheel.mouse_y);
                        Vector2 delta = new Vector2(@event.wheel.x, @event.wheel.y);

                        _mousePosition = position;
                        _mouseWheelDelta = delta;

                        IWindowHost? childHost = FindHostForEvent(host, position);
                        if (childHost != null)
                        {
                            IInteractable? interactable = FindTopNodeAtPosition(childHost, position);
                            if (interactable != null)
                            {
                                Vector2 mousePosition = position;
                                if (interactable.Host != null)
                                {
                                    mousePosition -= interactable.Host.ContentMetrics.Position.AsVector2();
                                }

                                FireEventForInteractable(interactable, UIEventType.MouseWheel, new UIMouseEvent { Position = mousePosition, Delta = delta });
                            }
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    {
                        Vector2 position = new Vector2(@event.button.x, @event.button.y);
                        MouseButton button = PointerDevice.TranslateButton(@event.button.Button);

                        _mousePosition = position;

                        if (button != MouseButton.Unknown)
                        {
                            IWindowHost? childHost = FindHostForEvent(host, position);
                            if (childHost != null)
                            {
                                IInteractable? interactable = FindTopNodeAtPosition(childHost, position);
                                if (interactable != null)
                                {
                                    OnInteractablePressed(interactable, button);
                                    ChangeInteractableFocus(interactable, button);
                                }
                            }
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    {
                        Vector2 position = new Vector2(@event.button.x, @event.button.y);
                        MouseButton button = PointerDevice.TranslateButton(@event.button.Button);

                        _mousePosition = position;

                        if (button != MouseButton.Unknown)
                        {
                            IWindowHost? childHost = FindHostForEvent(host, position);
                            if (childHost != null)
                            {
                                IInteractable? interactable = FindTopNodeAtPosition(childHost, position);
                                OnInteractableReleased(interactable, button);
                            }
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    {
                        KeyCode key = KeyboardDevice.TranslateKey(@event.key.key);
                        bool isRepeating = @event.key.repeat;

                        IWindowHost? childHost = FindHostForEvent(host, _mousePosition);
                        if (childHost != null)
                        {
                            IInteractable? interactable = FindTopNodeAtPosition(childHost, _mousePosition);
                            if (interactable != null)
                            {
                                FireEventForInteractable(interactable, UIEventType.KeyDown, new UIKeyEvent { Key = key, IsRepeating = isRepeating });
                            }
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_KEY_UP:
                    {
                        KeyCode key = KeyboardDevice.TranslateKey(@event.key.key);

                        IWindowHost? childHost = FindHostForEvent(host, _mousePosition);
                        if (childHost != null)
                        {
                            IInteractable? interactable = FindTopNodeAtPosition(childHost, _mousePosition);
                            if (interactable != null)
                            {
                                FireEventForInteractable(interactable, UIEventType.KeyUp, new UIKeyEvent { Key = key, IsRepeating = false });
                            }
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                case SDL_EventType.SDL_EVENT_WINDOW_HIDDEN:
                    {
                        if (_currentWindow != null && _currentWindow.WindowId == (uint)@event.window.windowID)
                        {
                            ChangeMouseHover(null, null);
                        }
                        break;
                    }
            }
        }

        private static IInteractable? FindTopNodeAtPosition(IWindowHost host, Vector2 position)
        {
            position -= host.ContentMetrics.Position.AsVector2();

            IInteractable startInteractable = host.GetInteractable(position);
            if (startInteractable == host)
                return startInteractable;

            return SearchRecursive(position, host.ActiveWindow!.RootElement);

            static IInteractable? SearchRecursive(Vector2 position, UIElement element)
            {
                bool hasScroll = element.ScrollPosition != Vector2.Zero;
                Vector2 childPosition = hasScroll ? position + element.ScrollPosition : position;

                for (int i = element.Children.Count - 1; i >= 0; --i)
                {
                    UIElement child = element.Children[i];
                    if (child.IsEnabled && child.IsActive && child.ElementTreeBounds.IsWithin(childPosition))
                    {
                        IInteractable? ret = SearchRecursive(childPosition, child);
                        if (ret != null)
                            return ret;
                    }
                }

                bool isIntersecting = element.Shape?.Intersects(position) ?? element.PixelCoordinates.IsWithin(position);
                return isIntersecting ? element.GetInteractable(position) : null;
            }
        }

        private static IWindowHost? FindHostForEvent(IWindowHost windowHost, Vector2 position)
        {
            if (windowHost.ContentMetrics.AsBoundaries().IsWithin(position))
                return windowHost;

            if (windowHost is IWindowDockHost dockHost)
            {
                foreach (IWindowHost dockedHost in dockHost.Hosts)
                {
                    if (dockedHost.HostMetrics.AsBoundaries().IsWithin(position))
                        return FindHostForEvent(dockedHost, position);
                }
            }

            return null;
        }

        private record struct MouseHeldData(IInteractable? Interactable, bool IsHeld, IInteractable? WithFocus);
        private record struct MouseDragData(EventDispatcher Dispatcher, bool IsDragActive, Vector2 StartPosition);
    }
}
