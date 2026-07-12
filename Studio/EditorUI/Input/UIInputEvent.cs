using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Input.Devices;

namespace EditorUI.Input
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct UIInputEvent
    {
        [FieldOffset(0)] public readonly UIInputEventType EventType;

        [FieldOffset(1)] public readonly UIMouseInputEvent Mouse;
        [FieldOffset(1)] public readonly UIDragInputEvent Drag;
        [FieldOffset(1)] public readonly UIKeyInputEvent Key;

        public UIInputEvent(UIInputEventType eventType, UIMouseInputEvent inputEventData)
        {
            EventType = eventType;
            Mouse = inputEventData;
        }

        public UIInputEvent(UIInputEventType eventType, UIDragInputEvent inputEventData)
        {
            EventType = eventType;
            Drag = inputEventData;
        }

        public UIInputEvent(UIInputEventType eventType, UIKeyInputEvent inputEventData)
        {
            EventType = eventType;
            Key = inputEventData;
        }
    }

    public enum UIInputEventType : byte
    {
        Unknown = 0,

        // Mouse
        MouseMotion,
        MouseWheel,
        MouseEnter,
        MouseLeave,
        MouseDown,
        MouseUp,
        MousePress,

        // Focus
        FocusGained,
        FocusLost,

        // Dragging
        DragBegin,
        DragUpdate,
        DragEnd,

        // Keyboard
        KeyDown,
        KeyUp
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct UIMouseInputEvent
    {
        [FieldOffset(0)] public readonly Vector2 Position;
        [FieldOffset(8)] public readonly Vector2 Delta;
        [FieldOffset(8)] public readonly MouseButton Button;
        [FieldOffset(9)] public readonly byte Click;

        public UIMouseInputEvent(Vector2 position, Vector2 delta)
        {
            Position = position;
            Delta = delta;
        }

        public UIMouseInputEvent(Vector2 position, MouseButton button, byte click)
        {
            Position = position;
            Button = button;
            Click = click;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct UIDragInputEvent
    {
        [FieldOffset(0)] public readonly MouseButton Button;
        [FieldOffset(1)] public readonly Vector2 Position;
        [FieldOffset(9)] public readonly Vector2 Delta;

        public UIDragInputEvent(MouseButton button, Vector2 position, Vector2 delta)
        {
            Button = button;
            Position = position;
            Delta = delta;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct UIKeyInputEvent
    {
        [FieldOffset(0)] public readonly KeyCode Key;
        [FieldOffset(1)] public readonly bool IsRepeating;

        public UIKeyInputEvent(KeyCode key, bool isRepeating)
        {
            Key = key;
            IsRepeating = isRepeating;
        }
    }
}
