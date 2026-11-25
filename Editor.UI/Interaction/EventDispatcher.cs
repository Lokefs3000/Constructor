using Editor.UI.Elements;
using Primary.Input.Devices;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Interaction
{
    public sealed class EventDispatcher
    {
        private readonly UIInteractionManager _manager;

        //drag callbacks
        private MouseButton _dragStartButton;
        private Vector2 _dragStartPosition;
        private Action<OnDragEventArgs>? _onDragCallback;
        private Action<OnDragEventArgs>? _onDragEndCallback;

        internal EventDispatcher(UIInteractionManager manager)
        {
            _manager = manager;

            _dragStartButton = MouseButton.Unknown;
            _dragStartPosition = Vector2.Zero;
            _onDragCallback = null;
            _onDragEndCallback = null;
        }

        internal void SetupForNewDrag(IInteractable interactable, MouseButton button, Vector2 startPosition, Vector2 position)
        {
            OnDragStartContext context = new OnDragStartContext(interactable, this, button, startPosition);
            if (interactable is UIElement element)
                element.Invoke_OnDragStart(context);

            _dragStartButton = button;
            _dragStartPosition = startPosition;

            _manager.FireEventForInteractable(interactable, UIEventType.DragBegin, new UIDragEvent { Button = button, Position = position, Delta = position - startPosition });

            _onDragCallback?.Invoke(new OnDragEventArgs(position, position - startPosition));
        }

        internal void EndCurrentDrag(IInteractable interactable, MouseButton button, Vector2 position)
        {
            _manager.FireEventForInteractable(interactable, UIEventType.DragEnd, new UIDragEvent { Button = button, Position = position, Delta = position - _dragStartPosition });

            _onDragEndCallback?.Invoke(new OnDragEventArgs(position, position - _dragStartPosition));

            _dragStartButton = MouseButton.Unknown;
            _onDragCallback = null;
            _onDragEndCallback = null;
        }

        internal void UpdateCurrentDrag(IInteractable interactable, Vector2 position)
        {
            _manager.FireEventForInteractable(interactable, UIEventType.DragUpdate, new UIDragEvent { Button = _dragStartButton, Position = position, Delta = position - _dragStartPosition });

            _onDragCallback?.Invoke(new OnDragEventArgs(position, position - _dragStartPosition));
        }

        internal void SetDragCallback(Action<OnDragEventArgs> callback) => _onDragCallback = callback;
        internal void SetDragEndCallback(Action<OnDragEventArgs> callback) => _onDragEndCallback = callback;

        internal MouseButton DragButton => _dragStartButton;
    }
}
