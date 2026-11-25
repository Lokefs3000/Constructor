using CommunityToolkit.Diagnostics;
using Editor.UI.Elements;
using Primary.Input.Devices;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Interaction
{
    public readonly record struct OnDragStartContext(IInteractable Interactable, EventDispatcher Dispatcher, MouseButton Button, Vector2 Position)
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
