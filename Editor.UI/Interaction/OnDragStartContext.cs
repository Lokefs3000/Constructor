using CommunityToolkit.Diagnostics;
using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Interaction
{
    public readonly record struct OnDragStartContext(UIElement Element, EventDispatcher Dispatcher)
    {
        public void SetDragCallback(Action<OnDragEventArgs> callback)
        {
            Guard.IsNotNull(callback);
            Dispatcher.SetDragCallback(callback);
        }

        public void SetDragEndCallback(Action<OnDragEventArgs> callback)
        {
            Guard.IsNotNull(callback);
            Dispatcher.SetDragEndCallback(callback);
        }
    }

    public readonly record struct OnDragEventArgs(Vector2 Position, Vector2 Delta);
}
