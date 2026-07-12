using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Widgets;

namespace EditorUI.Input
{
    public interface IInteractable
    {
        public IInteractable GetInteractable(Vector2 point);

        public void HandleEventSelf(ref readonly UIInputEvent inputEvent);

        public IInteractionShape? Shape { get; }
        public WidgetInputState InputState { get; }
    }
}
