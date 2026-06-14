using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Interaction
{
    public interface IInteractable
    {
        public IWindowHost? Host { get; }
        public IInteractionShape? Shape { get; }

        public IInteractable GetInteractable(Vector2 point);

        public void HandleEvent(ref readonly UIEvent @event);
    }
}
