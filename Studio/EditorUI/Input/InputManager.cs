using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Polling;
using Primary.Windowing;
using SDL;
using TerraFX.Interop.Windows;

namespace EditorUI.Input
{
    public sealed class InputManager : IEventHandler
    {
        private Dictionary<uint, IInputDispatcher> _inputDispatcher;
        private uint _lastWindowId;

        private Vector2 _currentMousePosition;

        private FocusedItem _currentHoveredInteractable;
        private MouseButtonFocus[] _mouseButtonFocuses;

        private MouseDragState[] _currentDragStates;

        internal InputManager()
        {
            _inputDispatcher = new Dictionary<uint, IInputDispatcher>();
            _lastWindowId = uint.MaxValue;

            _currentMousePosition = Vector2.Zero;

            _currentHoveredInteractable = FocusedItem.Null;
            _mouseButtonFocuses = new MouseButtonFocus[3];

            _currentDragStates = new MouseDragState[3];

            Array.Fill(_mouseButtonFocuses, new MouseButtonFocus(FocusedItem.Null));
        }

        public void BindInputDispatcher(Window window, IInputDispatcher inputDispatcher)
        {
            ref IInputDispatcher? currentInputDispatcher = ref CollectionsMarshal.GetValueRefOrAddDefault(_inputDispatcher, window.WindowId, out bool exists);
            if (exists)
            {
                if (currentInputDispatcher == inputDispatcher)
                    return;

                UILog.Logger?.Warning("Failed to bind input dispatcher because a different one was bound to the same window previously");
            }
            else
            {
                currentInputDispatcher = inputDispatcher;
            }
        }

        public void UnbindInputDispatcher(Window window, IInputDispatcher inputDispatcher)
        {
            if (_inputDispatcher.TryGetValue(window.WindowId, out IInputDispatcher? currentInputDispatcher))
            {
                if (currentInputDispatcher == inputDispatcher)
                {
                    _inputDispatcher.Remove(window.WindowId);
                    return;
                }
                else
                {
                    UILog.Logger?.Debug("Cannot unbind input dispatcher because it is not currently bound to the window");
                }
            }
        }

        public void ForgetInteractable(IInteractable interactable)
        {
            if (_currentHoveredInteractable == interactable)
            {
                _currentHoveredInteractable = FocusedItem.Null;
            }

            for (int i = 0; i < _mouseButtonFocuses.Length; ++i)
            {
                ref MouseButtonFocus currentButtonFocus = ref _mouseButtonFocuses[i];
                if (currentButtonFocus.Interactable == interactable)
                {
                    currentButtonFocus.Interactable = FocusedItem.Null;
                }
            }

            for (int i = 0; i < _currentDragStates.Length; ++i)
            {
                ref MouseDragState currentDragStat = ref _currentDragStates[i];
                if (currentDragStat.Interactable == interactable)
                {
                    currentDragStat.IsDragActive = false;
                    currentDragStat.Interactable = FocusedItem.Null;
                }
            }

            OnInteractableForgotten?.Invoke(interactable);
        }

        public void Handle(ref readonly SDL_Event @event)
        {
            if (!s_eventTypesOfInterest.Contains(@event.Type))
                return;

            uint currentWindowId = (uint)@event.window.windowID;

            if (!_inputDispatcher.TryGetValue(currentWindowId, out IInputDispatcher? inputDispatcher))
                return;

            Window window = WindowManager.Instance.FindWindow(currentWindowId)!;
            _currentMousePosition = Vector2.Clamp((InputSystem.Pointer.GlobalMousePosition - window.Position).AsVector2(), Vector2.Zero, window.ClientSize.AsVector2());

            InputDispatcherRoot root = inputDispatcher.GetInteractable(_currentMousePosition);
            if (root.Interactable != null)
            {
                PropagateEventForTree(in @event, root.Interactable, root.Offset);
            }
        }

        private void PropagateEventForTree(ref readonly SDL_Event @event, IInteractable rootInteractable, Vector2 offset)
        {
            uint currentWindowId = (uint)@event.window.windowID;
            Vector2 globalMousePosition = InputSystem.Pointer.GlobalMousePosition.AsVector2();

            switch (@event.Type)
            {
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    {
                        Vector2 point = new Vector2(@event.motion.x, @event.motion.y) + offset;
                        Vector2 delta = new Vector2(@event.motion.xrel, @event.motion.yrel);

                        (IInteractable ? interactable, Vector2 offsetAtLower) = FindInteractableAtTop(rootInteractable, point, Vector2.Zero);
                        point += offsetAtLower;

                        if (interactable != null)
                        {
                            FireEventForInteractable(interactable, UIInputEventType.MouseMotion, new UIMouseInputEvent(point, delta));
                        }

                        if (_currentHoveredInteractable != interactable)
                        {
                            if (_currentHoveredInteractable != null)
                            {
                                FireEventForInteractable(_currentHoveredInteractable.Item!, UIInputEventType.MouseLeave, new UIMouseInputEvent(point, delta));
                            }

                            if (interactable != null)
                            {
                                FireEventForInteractable(interactable, UIInputEventType.MouseEnter, new UIMouseInputEvent(point, delta));
                                _currentHoveredInteractable = new FocusedItem(interactable, currentWindowId);
                            }
                            else
                            {
                                _currentHoveredInteractable = FocusedItem.Null;
                            }
                        }

                        for (int i = 0; i < _currentDragStates.Length; ++i)
                        {
                            ref MouseDragState dragState = ref _currentDragStates[i];
                            if (dragState.Interactable != null)
                            {
                                Vector2 dragDelta = globalMousePosition - dragState.StartPosition;
                                if (dragState.IsDragActive)
                                {
                                    FireEventForInteractable(dragState.Interactable.Item!, UIInputEventType.DragUpdate, new UIDragInputEvent((MouseButton)i, globalMousePosition, dragDelta));
                                }
                                else if (dragDelta != Vector2.Zero)
                                {
                                    dragState.IsDragActive = true;
                                    FireEventForInteractable(dragState.Interactable.Item!, UIInputEventType.DragBegin, new UIDragInputEvent((MouseButton)i, globalMousePosition, dragDelta));
                                }
                            }
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    {
                        Vector2 point = new Vector2(@event.wheel.mouse_x, @event.wheel.mouse_y) + offset;
                        Vector2 delta = new Vector2(@event.wheel.x, @event.wheel.y);

                        (IInteractable? interactable, Vector2 offsetAtLower) = FindInteractableAtTop(rootInteractable, point, Vector2.Zero);
                        point += offsetAtLower;

                        if (interactable != null)
                        {
                            FireEventForInteractable(interactable, UIInputEventType.MouseWheel, new UIMouseInputEvent(point, delta));
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    {
                        Vector2 point = new Vector2(@event.button.x, @event.button.y) + offset;
                        MouseButton button = PointerDevice.TranslateButton(@event.button.Button);

                        (IInteractable? interactable, Vector2 offsetAtLower) = FindInteractableAtTop(rootInteractable, point, Vector2.Zero);
                        point += offsetAtLower;

                        if (interactable != null)
                        {
                            ref MouseButtonFocus currentButtonFocus = ref _mouseButtonFocuses[(int)button];
                            if (currentButtonFocus.Interactable != null)
                                UILog.Logger?.Warning("Trying to start press on interactable while other interactable is already active! The current active interactable will not have any futher events fired");

                            currentButtonFocus.Interactable = new FocusedItem(interactable, currentWindowId);
                            FireEventForInteractable(interactable, UIInputEventType.MouseDown, new UIMouseInputEvent(point, button, @event.button.clicks));

                            ref MouseDragState currentDragState = ref _currentDragStates[(int)button];
                            if (currentDragState.Interactable != null && currentDragState.IsDragActive)
                                UILog.Logger?.Warning("Trying to setup for drag on interactable while other interactable is already setup! The current dragged interactable will not have any further events fired");

                            currentDragState.Interactable = currentButtonFocus.Interactable;
                            currentDragState.StartPosition = globalMousePosition;
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    {
                        Vector2 point = new Vector2(@event.button.x, @event.button.y) + offset;
                        MouseButton button = PointerDevice.TranslateButton(@event.button.Button);

                        ref MouseButtonFocus currentButtonFocus = ref _mouseButtonFocuses[(int)button];
                        if (currentButtonFocus.Interactable != null)
                        {
                            FireEventForInteractable(currentButtonFocus.Interactable.Item!, UIInputEventType.MouseUp, new UIMouseInputEvent(point, button, @event.button.clicks));

                            (IInteractable? interactable, Vector2 offsetAtLower) = FindInteractableAtTop(rootInteractable, point, Vector2.Zero);
                            point += offsetAtLower;

                            if (currentButtonFocus.Interactable == interactable)
                            {
                                FireEventForInteractable(currentButtonFocus.Interactable.Item!, UIInputEventType.MousePress, new UIMouseInputEvent(point, button, @event.button.clicks));
                            }

                            currentButtonFocus.Interactable = FocusedItem.Null;
                        }

                        ref MouseDragState currentDragState = ref _currentDragStates[(int)button];
                        if (currentDragState.Interactable != null)
                        {
                            if (currentDragState.IsDragActive)
                            {
                                FireEventForInteractable(currentDragState.Interactable.Item!, UIInputEventType.DragEnd, new UIDragInputEvent(button, globalMousePosition, globalMousePosition - currentDragState.StartPosition));
                                currentDragState.IsDragActive = false;
                            }

                            currentDragState.Interactable = FocusedItem.Null;
                        }

                        break;
                    }

                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    {
                        Vector2 point = _currentMousePosition + offset;

                        (IInteractable? interactable, Vector2 offsetAtLower) = FindInteractableAtTop(rootInteractable, point, Vector2.Zero);
                        point += offsetAtLower;

                        if (interactable != null)
                        {
                            KeyCode keyCode = KeyboardDevice.TranslateKey(@event.key.key);
                            bool isRepeat = @event.key.repeat;

                            FireEventForInteractable(interactable, UIInputEventType.KeyDown, new UIKeyInputEvent(keyCode, isRepeat));
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_KEY_UP:
                    {
                        Vector2 point = _currentMousePosition + offset;

                        (IInteractable? interactable, Vector2 offsetAtLower) = FindInteractableAtTop(rootInteractable, point, Vector2.Zero);
                        point += offsetAtLower;

                        if (interactable != null)
                        {
                            KeyCode keyCode = KeyboardDevice.TranslateKey(@event.key.key);
                            bool isRepeat = @event.key.repeat;

                            FireEventForInteractable(interactable, UIInputEventType.KeyUp, new UIKeyInputEvent(keyCode, isRepeat));
                        }

                        break;
                    }

                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                    {
                        if (_currentHoveredInteractable != null && _currentHoveredInteractable.WindowSourceId == currentWindowId)
                        {
                            Vector2 point = _currentMousePosition;

                            Window? window = WindowManager.Instance.FindWindow((uint)@event.window.windowID);
                            if (window != null)
                            {
                                Boundaries usableBoundaries = window.Display.UsableBoundaries.AsBoundaries();
                                point = Boundaries.Contain(usableBoundaries, InputSystem.Pointer.GlobalMousePosition.AsVector2());
                            }

                            FireEventForInteractable(_currentHoveredInteractable.Item!, UIInputEventType.MouseLeave, new UIMouseInputEvent(point + offset, Vector2.Zero));
                            _currentHoveredInteractable = FocusedItem.Null;
                        }

                        break;
                    }
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    {
                        if (_currentHoveredInteractable.IsValidAndForThisWindow(currentWindowId))
                        {
                            FireEventForInteractable(_currentHoveredInteractable.Item!, UIInputEventType.MouseLeave, new UIMouseInputEvent(_currentMousePosition + offset, Vector2.Zero));
                            _currentHoveredInteractable = FocusedItem.Null;
                        }

                        for (int i = 0; i < _mouseButtonFocuses.Length; i++)
                        {
                            ref MouseButtonFocus focus = ref _mouseButtonFocuses[i];
                            if (focus.Interactable.IsValidAndForThisWindow(currentWindowId))
                            {
                                FireEventForInteractable(focus.Interactable.Item!, UIInputEventType.MouseUp, new UIMouseInputEvent(_currentMousePosition + offset, (MouseButton)i, 1));
                                focus.Interactable = FocusedItem.Null;
                            }
                        }

                        for (int i = 0; i < _currentDragStates.Length; ++i)
                        {
                            ref MouseDragState dragState = ref _currentDragStates[i];
                            if (dragState.Interactable.IsValidAndForThisWindow(currentWindowId))
                            {
                                Vector2 dragDelta = globalMousePosition - dragState.StartPosition;
                                if (dragState.IsDragActive)
                                {
                                    FireEventForInteractable(dragState.Interactable.Item!, UIInputEventType.DragEnd, new UIDragInputEvent((MouseButton)i, globalMousePosition, dragDelta));
                                    dragState.IsDragActive = false;
                                }

                                dragState.Interactable = FocusedItem.Null;
                            }
                        }

                        if (_lastWindowId == currentWindowId)
                        {
                            _lastWindowId = uint.MaxValue;
                        }

                        break;
                    }
            }
        }

        private void FireEventForInteractable(IInteractable interactable, UIInputEventType eventType, UIMouseInputEvent inputEventData) => FireEventForInteractable(interactable, new UIInputEvent(eventType, inputEventData));
        private void FireEventForInteractable(IInteractable interactable, UIInputEventType eventType, UIDragInputEvent inputEventData) => FireEventForInteractable(interactable, new UIInputEvent(eventType, inputEventData));
        private void FireEventForInteractable(IInteractable interactable, UIInputEventType eventType, UIKeyInputEvent inputEventData) => FireEventForInteractable(interactable, new UIInputEvent(eventType, inputEventData));

        private void FireEventForInteractable(IInteractable interactable, UIInputEvent inputEvent) => interactable.HandleEventSelf(ref inputEvent);

        private (IInteractable? Interactable, Vector2 OffsetAtPoint) FindInteractableAtTop(IInteractable? interactable, Vector2 point, Vector2 offset)
        {
            if (interactable == null)
            {
                return (null, offset);
            }

            if (interactable.InputState != WidgetInputState.Swallow && interactable is Widget widget)
            {
                Vector2 thisPoint = point;
                Vector2 thisOffset = offset;

                if (widget is ScrollView scrollView)
                {
                    thisPoint -= scrollView.ScrollPosition;
                    thisOffset -= scrollView.ScrollPosition;
                }

                ROList<Widget> children = widget.Children;
                for (int i = children.Count - 1; i >= 0; --i)
                {
                    Widget child = children[i];
                    if (child.IsEnabled && child.InputState != WidgetInputState.Never && child.ComputedRect.IsWithin(thisPoint))
                    {
                        (IInteractable? foundLowerInTree, Vector2 offsetLowerInTree) = FindInteractableAtTop(child, thisPoint, thisOffset);
                        if (foundLowerInTree != null)
                            return (foundLowerInTree, offsetLowerInTree);
                    }
                }
            }

            return ((interactable.InputState != WidgetInputState.Passthrough && (interactable.Shape?.Intersects(point) ?? true)) ? interactable.GetInteractable(point) : null, offset);
        }

        public event Action<IInteractable>? OnInteractableForgotten;

        public IInteractable? HoveredInteractable => _currentHoveredInteractable.Item;

        private static readonly FrozenSet<SDL_EventType> s_eventTypesOfInterest = new HashSet<SDL_EventType>
        {
            SDL_EventType.SDL_EVENT_MOUSE_MOTION,
            SDL_EventType.SDL_EVENT_MOUSE_WHEEL,
            SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN,
            SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP,
            SDL_EventType.SDL_EVENT_KEY_DOWN,
            SDL_EventType.SDL_EVENT_KEY_UP,
            SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE,
            SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST
        }.ToFrozenSet();

        private record struct MouseButtonFocus(FocusedItem Interactable);
        private record struct MouseDragState(FocusedItem Interactable, Vector2 StartPosition, bool IsDragActive);

        private record struct FocusedItem(IInteractable? Item, uint WindowSourceId) : IEquatable<IInteractable?>
        {
            public readonly bool IsValidAndForThisWindow(uint windowId) => Item != null && WindowSourceId == windowId;

            [MemberNotNullWhen(true, nameof(Item))]
            public readonly bool Equals(IInteractable? other) => Item == other;

            [MemberNotNullWhen(true, nameof(Item))]
            public static bool operator ==(FocusedItem lhs, IInteractable? rhs) => lhs.Equals(rhs);
            [MemberNotNullWhen(false, nameof(Item))]
            public static bool operator !=(FocusedItem lhs, IInteractable? rhs) => !(lhs == rhs);

            public static FocusedItem Null => new FocusedItem(null, uint.MaxValue);
        }
    }
}
