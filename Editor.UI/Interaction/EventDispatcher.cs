using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Interaction
{
    public sealed class EventDispatcher
    {
        //drag callbacks
        private Vector2 _dragStartPosition;
        private Action<OnDragEventArgs>? _onDragCallback;
        private Action<OnDragEventArgs>? _onDragEndCallback;

        internal EventDispatcher()
        {
            _dragStartPosition = Vector2.Zero;
            _onDragCallback = null;
            _onDragEndCallback = null;
        }

        internal void SetupForNewDrag(UIElement element, Vector2 startPosition, Vector2 position)
        {
            OnDragStartContext context = new OnDragStartContext(element, this);
            element.Invoke_OnDragStart(context);

            _dragStartPosition = startPosition;

            if (_onDragCallback != null)
                _onDragCallback(new OnDragEventArgs(position, position - startPosition));
        }

        internal void EndCurrentDrag(UIElement element, Vector2 position)
        {
            if (_onDragEndCallback != null)
                _onDragEndCallback(new OnDragEventArgs(position, position - _dragStartPosition));

            _onDragCallback = null;
            _onDragEndCallback = null;
        }

        internal void UpdateCurrentDrag(UIElement element, Vector2 position)
        {
            if (_onDragCallback != null)
                _onDragCallback(new OnDragEventArgs(position, position - _dragStartPosition));
        }

        internal void SetDragCallback(Action<OnDragEventArgs> callback) => _onDragCallback = callback;
        internal void SetDragEndCallback(Action<OnDragEventArgs> callback) => _onDragEndCallback = callback;
    }
}
