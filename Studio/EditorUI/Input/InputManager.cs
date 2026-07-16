using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using EditorUI.Scheduling;
using EditorUI.Widgets;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Polling;
using Primary.Windowing;
using SDL;

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

        private FocusedItem[] _inputFocus;

        internal InputManager()
        {
            _inputDispatcher = new Dictionary<uint, IInputDispatcher>();
            _lastWindowId = uint.MaxValue;

            _currentMousePosition = Vector2.Zero;

            _currentHoveredInteractable = FocusedItem.Null;
            _mouseButtonFocuses = new MouseButtonFocus[3];

            _currentDragStates = new MouseDragState[3];

            _inputFocus = new FocusedItem[3];

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

            ForceInputUpdate();
            OnInteractableForgotten?.Invoke(interactable);
        }

        public void ForceInputUpdate()
        {
            UIManager.Instance.ActionScheduler.TryScheduleUnique(this, static (key, _) => ((InputManager)key).UpdateInputsForActive(), KnownPriorities.UpdateInput, null);
        }

        private void UpdateInputsForActive()
        {
            if (_lastWindowId == uint.MaxValue)
                _lastWindowId = WindowManager.Instance.PrimaryWindow!.WindowId;

            if (!_inputDispatcher.TryGetValue(_lastWindowId, out IInputDispatcher? inputDispatcher))
                return;

            Window window = WindowManager.Instance.FindWindow(_lastWindowId)!;
            _currentMousePosition = Vector2.Clamp((InputSystem.Pointer.GlobalMousePosition - window.Position).AsVector2(), Vector2.Zero, window.ClientSize.AsVector2());

            InputDispatcherRoot root = inputDispatcher.GetInteractable(_currentMousePosition);
            if (root.Interactable != null)
            {
                RentedList<InteractionQueueItem> interactionQueue = new RentedList<InteractionQueueItem>();
                FillInteractionQueue(ref interactionQueue, root.Interactable, _currentMousePosition + root.Offset);

                if (interactionQueue.IsEmpty)
                {
                    if (_currentHoveredInteractable.IsNotNull)
                    {
                        FireEventForInteractable(_currentHoveredInteractable.Item, UIInputEventType.MouseLeave, new UIMouseInputEvent(_currentHoveredInteractable.Hit, Vector2.Zero));
                        _currentHoveredInteractable = FocusedItem.Null;
                    }
                }
                else
                {
                    for (int i = 0; i < interactionQueue.Count; ++i)
                    {
                        InteractionQueueItem queueItem = interactionQueue[i];
                        if (i == 0)
                        {
                            if (_currentHoveredInteractable.Item != queueItem.Interactable)
                            {
                                if (_currentHoveredInteractable.IsNotNull)
                                    FireEventForInteractable(_currentHoveredInteractable.Item, UIInputEventType.MouseLeave, new UIMouseInputEvent(queueItem.InputPosition, Vector2.Zero));
                                FireEventForInteractable(queueItem.Interactable, UIInputEventType.MouseEnter, new UIMouseInputEvent(queueItem.InputPosition, Vector2.Zero));

                                _currentHoveredInteractable = new FocusedItem(queueItem.Interactable, _lastWindowId, queueItem.InputPosition);
                            }
                            else
                            {
                                _currentHoveredInteractable.Hit = queueItem.InputPosition;
                            }
                        }

                        if (FireEventForInteractable(queueItem.Interactable, UIInputEventType.MouseMotion, new UIMouseInputEvent(queueItem.InputPosition, Vector2.Zero)))
                        {
                            break;
                        }
                    }
                }

                interactionQueue.Dispose();
            }
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
            PropagateEventForTree(in @event, root.Interactable, root.Offset);
        }

        private void PropagateEventForTree(ref readonly SDL_Event @event, IInteractable? rootInteractable, Vector2 offset)
        {
            uint currentWindowId = (uint)@event.window.windowID;
            // Vector2 globalMousePosition = InputSystem.Pointer.GlobalMousePosition.AsVector2();

            switch (@event.Type)
            {
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    {
                        HandleEventForMouseMotion(in @event, currentWindowId, rootInteractable, _currentMousePosition + offset);
                        UpdateDragStates();
                        _lastWindowId = currentWindowId;
                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    {
                        HandleEventForMouseWheel(in @event, currentWindowId, rootInteractable, _currentMousePosition + offset);
                        _lastWindowId = currentWindowId;
                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    {
                        HandleEventForButtonDown(in @event, currentWindowId, rootInteractable, _currentMousePosition + offset);
                        _lastWindowId = currentWindowId;
                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    {
                        HandleEventForButtonUp(in @event, currentWindowId, rootInteractable, _currentMousePosition + offset);
                        _lastWindowId = currentWindowId;
                        break;
                    }
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    {
                        HandleEventForKeyDown(in @event, currentWindowId, rootInteractable, _currentMousePosition + offset);
                        _lastWindowId = currentWindowId;
                        break;
                    }
                case SDL_EventType.SDL_EVENT_KEY_UP:
                    {
                        HandleEventForKeyUp(in @event, currentWindowId, rootInteractable, _currentMousePosition + offset);
                        _lastWindowId = currentWindowId;
                        break;
                    }

                case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                    {
                        if (_currentHoveredInteractable.IsValidAndForThisWindow(currentWindowId))
                        {
                            FireEventForInteractable(_currentHoveredInteractable.Item, UIInputEventType.MouseLeave, new UIMouseInputEvent(_currentHoveredInteractable.Hit, Vector2.Zero));
                            _currentHoveredInteractable = FocusedItem.Null;
                        }

                        _lastWindowId = currentWindowId;
                        break;
                    }
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    {
                        if (_currentHoveredInteractable.IsValidAndForThisWindow(currentWindowId))
                        {
                            FireEventForInteractable(_currentHoveredInteractable.Item, UIInputEventType.MouseLeave, new UIMouseInputEvent(_currentHoveredInteractable.Hit, Vector2.Zero));
                            _currentHoveredInteractable = FocusedItem.Null;
                        }

                        for (int i = 0; i < _mouseButtonFocuses.Length; i++)
                        {
                            ref MouseButtonFocus focus = ref _mouseButtonFocuses[i];
                            if (focus.Interactable.IsValidAndForThisWindow(currentWindowId))
                            {
                                FireEventForInteractable(focus.Interactable.Item!, UIInputEventType.MouseUp, new UIMouseInputEvent(focus.Interactable.Hit, (MouseButton)i, 1));
                                focus.Interactable = FocusedItem.Null;
                            }
                        }

                        for (int i = 0; i < _currentDragStates.Length; ++i)
                        {
                            ref MouseDragState dragStateData = ref _currentDragStates[i];
                            if (dragStateData.Interactable.IsValidAndForThisWindow(currentWindowId))
                            {
                                if (dragStateData.Interactable.IsNotNull)
                                {
                                    Vector2 dragPosition = InputSystem.Pointer.GlobalMousePosition.AsVector2();
                                    Vector2 dragDelta = dragPosition - dragStateData.StartPosition.AsVector2();
                                    FireEventForInteractable(dragStateData.Interactable.Item, UIInputEventType.DragEnd, new UIDragInputEvent((MouseButton)i, dragPosition, dragDelta));
                                }

                                dragStateData.Interactable = FocusedItem.Null;
                                dragStateData.IsDragActive = false;
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

        private void HandleEventForMouseMotion(ref readonly SDL_Event eventData, uint currentWindowId, IInteractable? rootInteractable, Vector2 hitPosition)
        {
            RentedList<InteractionQueueItem> interactionQueue = new RentedList<InteractionQueueItem>();
            if (rootInteractable != null)
                FillInteractionQueue(ref interactionQueue, rootInteractable, hitPosition);

            Vector2 delta = new Vector2(eventData.motion.xrel, eventData.motion.yrel);

            if (interactionQueue.IsEmpty)
            {
                if (_currentHoveredInteractable.IsNotNull)
                {
                    FireEventForInteractable(_currentHoveredInteractable.Item, UIInputEventType.MouseLeave, new UIMouseInputEvent(_currentHoveredInteractable.Hit, delta));
                    _currentHoveredInteractable = FocusedItem.Null;
                }
            }
            else
            {
                for (int i = 0; i < interactionQueue.Count; ++i)
                {
                    InteractionQueueItem queueItem = interactionQueue[i];
                    if (i == 0)
                    {
                        if (_currentHoveredInteractable.Item != queueItem.Interactable)
                        {
                            if (_currentHoveredInteractable.IsNotNull)
                                FireEventForInteractable(_currentHoveredInteractable.Item, UIInputEventType.MouseLeave, new UIMouseInputEvent(queueItem.InputPosition, delta));
                            FireEventForInteractable(queueItem.Interactable, UIInputEventType.MouseEnter, new UIMouseInputEvent(queueItem.InputPosition, delta));

                            _currentHoveredInteractable = new FocusedItem(queueItem.Interactable, currentWindowId, queueItem.InputPosition);
                        }
                        else
                        {
                            _currentHoveredInteractable.Hit = queueItem.InputPosition;
                        }
                    }

                    if (FireEventForInteractable(queueItem.Interactable, UIInputEventType.MouseMotion, new UIMouseInputEvent(queueItem.InputPosition, delta)))
                    {
                        break;
                    }
                }
            }

            interactionQueue.Dispose();
        }

        private void UpdateDragStates()
        {
            Vector2 dragPosition = InputSystem.Pointer.GlobalMousePosition.AsVector2();
            for (int i = 0; i < _currentDragStates.Length; ++i)
            {
                ref MouseButtonFocus buttonFocusData = ref _mouseButtonFocuses[i];
                ref MouseDragState dragStateData = ref _currentDragStates[i];

                if (buttonFocusData.Interactable.IsNotNull && dragStateData.Interactable.IsNotNull)
                {
                    Vector2 dragDelta = dragPosition - dragStateData.StartPosition.AsVector2();
                    if (dragStateData.IsDragActive)
                    {
                        FireEventForInteractable(dragStateData.Interactable.Item, UIInputEventType.DragUpdate, new UIDragInputEvent((MouseButton)i, dragPosition, dragDelta));
                    }
                    else
                    {
                        FireEventForInteractable(dragStateData.Interactable.Item, UIInputEventType.DragBegin, new UIDragInputEvent((MouseButton)i, dragPosition, dragDelta));
                        dragStateData.IsDragActive = true;
                    }
                }
            }
        }

        private void HandleEventForMouseWheel(ref readonly SDL_Event eventData, uint currentWindowId, IInteractable? rootInteractable, Vector2 hitPosition)
        {
            RentedList<InteractionQueueItem> interactionQueue = new RentedList<InteractionQueueItem>();
            if (rootInteractable != null)
                FillInteractionQueue(ref interactionQueue, rootInteractable, hitPosition);

            Vector2 delta = new Vector2(eventData.wheel.x, eventData.wheel.y);
            for (int i = 0; i < interactionQueue.Count; ++i)
            {
                InteractionQueueItem queueItem = interactionQueue[i];
                if (FireEventForInteractable(queueItem.Interactable, UIInputEventType.MouseWheel, new UIMouseInputEvent(queueItem.InputPosition, delta)))
                {
                    break;
                }
            }

            interactionQueue.Dispose();
        }

        private void HandleEventForButtonDown(ref readonly SDL_Event eventData, uint currentWindowId, IInteractable? rootInteractable, Vector2 hitPosition)
        {
            RentedList<InteractionQueueItem> interactionQueue = new RentedList<InteractionQueueItem>();
            if (rootInteractable != null)
                FillInteractionQueue(ref interactionQueue, rootInteractable, hitPosition);

            MouseButton mouseButton = PointerDevice.TranslateButton(eventData.button.Button);
            if (mouseButton != MouseButton.Unknown && !interactionQueue.IsEmpty)
            {
                InteractionQueueItem itemToGetFocus = interactionQueue[0];
                FireEventForInteractable(itemToGetFocus.Interactable, UIInputEventType.MouseDown, new UIMouseInputEvent(itemToGetFocus.InputPosition, mouseButton, eventData.button.clicks));

                ref MouseButtonFocus buttonFocusData = ref _mouseButtonFocuses[(int)mouseButton];
                buttonFocusData.Interactable = new FocusedItem(itemToGetFocus.Interactable, currentWindowId, itemToGetFocus.InputPosition);

                ref MouseDragState dragStateData = ref _currentDragStates[(int)mouseButton];
                dragStateData.Interactable = new FocusedItem(itemToGetFocus.Interactable, currentWindowId, itemToGetFocus.InputPosition);
                dragStateData.StartPosition = InputSystem.Pointer.GlobalMousePosition;
                dragStateData.IsDragActive = false;
            }

            interactionQueue.Dispose();
        }

        private void HandleEventForButtonUp(ref readonly SDL_Event eventData, uint currentWindowId, IInteractable? rootInteractable, Vector2 hitPosition)
        {
            RentedList<InteractionQueueItem> interactionQueue = new RentedList<InteractionQueueItem>();
            if (rootInteractable != null)
                FillInteractionQueue(ref interactionQueue, rootInteractable, hitPosition);

            MouseButton mouseButton = PointerDevice.TranslateButton(eventData.button.Button);
            if (mouseButton != MouseButton.Unknown && !interactionQueue.IsEmpty)
            {
                InteractionQueueItem itemToReleaseFocus = interactionQueue[0];

                ref MouseButtonFocus buttonFocusData = ref _mouseButtonFocuses[(int)mouseButton];
                if (buttonFocusData.Interactable.IsNotNull)
                {
                    FireEventForInteractable(buttonFocusData.Interactable.Item, UIInputEventType.MouseUp, new UIMouseInputEvent(itemToReleaseFocus.InputPosition, mouseButton, eventData.button.clicks));

                    if (itemToReleaseFocus.Interactable == buttonFocusData.Interactable.Item)
                    {
                        FireEventForInteractable(buttonFocusData.Interactable.Item, UIInputEventType.MousePress, new UIMouseInputEvent(itemToReleaseFocus.InputPosition, mouseButton, eventData.button.clicks));
                    }

                    buttonFocusData.Interactable = FocusedItem.Null;
                }

                ref FocusedItem inputFocus = ref _inputFocus[(int)mouseButton];
                if (inputFocus.IsNotNull && inputFocus.Item != itemToReleaseFocus.Interactable)
                {
                    FireEventForInteractable(inputFocus.Item, UIInputEventType.FocusLost);
                }

                FireEventForInteractable(itemToReleaseFocus.Interactable, UIInputEventType.FocusGained);
                inputFocus = new FocusedItem(itemToReleaseFocus.Interactable, currentWindowId, itemToReleaseFocus.InputPosition);

                ref MouseDragState dragStateData = ref _currentDragStates[(int)mouseButton];
                if (dragStateData.IsDragActive)
                {
                    if (dragStateData.Interactable.IsNotNull)
                    {
                        Vector2 dragPosition = InputSystem.Pointer.GlobalMousePosition.AsVector2();
                        Vector2 dragDelta = dragPosition - dragStateData.StartPosition.AsVector2();
                        FireEventForInteractable(dragStateData.Interactable.Item, UIInputEventType.DragEnd, new UIDragInputEvent(mouseButton, dragPosition, dragDelta));
                    }

                    dragStateData.Interactable = FocusedItem.Null;
                    dragStateData.IsDragActive = false;
                }
            }

            interactionQueue.Dispose();
        }

        private void HandleEventForKeyDown(ref readonly SDL_Event eventData, uint currentWindowId, IInteractable? rootInteractable, Vector2 hitPosition)
        {
            ref FocusedItem inputFocus = ref _inputFocus[(int)MouseButton.Left];
            if (inputFocus.IsNotNull)
            {
                KeyCode key = KeyboardDevice.TranslateKey(eventData.key.key);
                if (key != KeyCode.Unknown)
                {
                    FireEventForInteractable(inputFocus.Item, UIInputEventType.KeyDown, new UIKeyInputEvent(key, eventData.key.repeat));
                }
            }
        }

        private void HandleEventForKeyUp(ref readonly SDL_Event eventData, uint currentWindowId, IInteractable? rootInteractable, Vector2 hitPosition)
        {
            ref FocusedItem inputFocus = ref _inputFocus[(int)MouseButton.Left];
            if (inputFocus.IsNotNull)
            {
                KeyCode key = KeyboardDevice.TranslateKey(eventData.key.key);
                if (key != KeyCode.Unknown)
                {
                    FireEventForInteractable(inputFocus.Item, UIInputEventType.KeyUp, new UIKeyInputEvent(key, eventData.key.repeat));
                }
            }
        }

        private bool FireEventForInteractable(IInteractable interactable, UIInputEventType eventType) => FireEventForInteractable(interactable, new UIInputEvent(eventType));
        private bool FireEventForInteractable(IInteractable interactable, UIInputEventType eventType, UIMouseInputEvent inputEventData) => FireEventForInteractable(interactable, new UIInputEvent(eventType, inputEventData));
        private bool FireEventForInteractable(IInteractable interactable, UIInputEventType eventType, UIDragInputEvent inputEventData) => FireEventForInteractable(interactable, new UIInputEvent(eventType, inputEventData));
        private bool FireEventForInteractable(IInteractable interactable, UIInputEventType eventType, UIKeyInputEvent inputEventData) => FireEventForInteractable(interactable, new UIInputEvent(eventType, inputEventData));

        private bool FireEventForInteractable(IInteractable interactable, UIInputEvent inputEvent)
        {
            try
            {
                return interactable.HandleEventSelf(ref inputEvent);
            }
            catch (Exception ex)
            {
                UILog.Logger?.Error(ex, "Failed to fire event for interactable");
                return false;
            }
        }

        private static bool FillInteractionQueue(ref RentedList<InteractionQueueItem> queue, IInteractable interactable, Vector2 inputPosition)
        {
            WidgetInputState inputState = interactable.InputState;
            bool hadDescendentAtPosition = false;

            // Assume first interactable can always be interacted with
            if (inputState != WidgetInputState.Swallow && interactable is Widget widget && widget.Children.Count > 0)
            {
                Vector2 inputPositionForChildren = inputPosition;
                if (widget is ScrollView scrollView)
                {
                    inputPositionForChildren += scrollView.ScrollPosition;
                }

                ROList<Widget> children = widget.Children;
                for (int i = children.Count - 1; i >= 0; --i)
                {
                    Widget childWidget = children[i];
                    if (!childWidget.IsEnabled)
                        continue;

                    WidgetInputState childInputState = childWidget.InputState;

                    if (childInputState != WidgetInputState.Never)
                    {
                        hadDescendentAtPosition = FillInteractionQueue(ref queue, childWidget, inputPositionForChildren);
                        if (hadDescendentAtPosition)
                        {
                            if (inputState == WidgetInputState.Intercept)
                                break;
                            else
                                return true;
                        }
                    }
                }
            }

            if (inputState != WidgetInputState.Passthrough && (interactable.Shape?.Intersects(inputPosition) ?? true))
            {
                IInteractable gotten = interactable.GetInteractable(inputPosition);

                queue.Add(new InteractionQueueItem(gotten, inputPosition));
                if (inputState == WidgetInputState.Intercept && gotten != interactable)
                    queue.Add(new InteractionQueueItem(interactable, inputPosition));
                return true;
            }

            return hadDescendentAtPosition;
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
        private record struct MouseDragState(FocusedItem Interactable, Int2 StartPosition, bool IsDragActive);

        private record struct FocusedItem(IInteractable? Item, uint WindowSourceId, Vector2 Hit) : IEquatable<IInteractable?>
        {
            [MemberNotNullWhen(true, nameof(Item))]
            public readonly bool IsValidAndForThisWindow(uint windowId) => Item != null && WindowSourceId == windowId;

            public readonly bool Equals(IInteractable? other) => Item == other;

            [MemberNotNullWhen(false, nameof(Item))]
            public readonly bool IsNull => Item == null;

            [MemberNotNullWhen(true, nameof(Item))]
            public readonly bool IsNotNull => Item != null;

            public static bool operator ==(FocusedItem lhs, IInteractable? rhs) => lhs.Equals(rhs);
            public static bool operator !=(FocusedItem lhs, IInteractable? rhs) => !(lhs == rhs);

            public static FocusedItem Null => new FocusedItem(null, uint.MaxValue, Vector2.Zero);
        }

        private readonly record struct InteractionQueueItem(IInteractable Interactable, Vector2 InputPosition);
    }
}
