using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Windowing;

namespace EditorUI.Input
{
    public interface IInputDispatcher
    {
        public InputDispatcherRoot GetInteractable(Vector2 point);

        public Window Window { get; }
    }

    public readonly record struct InputDispatcherRoot(IInteractable? Interactable, Vector2 Offset)
    {
        public static InputDispatcherRoot Null => new InputDispatcherRoot(null, Vector2.Zero);
    }
}
