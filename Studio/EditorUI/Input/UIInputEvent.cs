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

        public UIInputEvent(UIInputEventType eventType)
        {
            EventType = eventType;
        }

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

    /// <remarks>
    /// There are 3 types of focus an interactable can have at any given time
    /// <list type="bullet">
    /// <item>
    /// Hover: The interactable currently under the mouse
    /// </item>
    /// <item>
    /// Button: The interactable under the mouse when the button was pressed (There is a unique focus for context for each button)
    /// </item>
    /// <item>
    /// Input: The interactable that was under the mouse when the left mouse button was both pressed and released
    /// </item>
    /// </list>
    /// </remarks>
    public enum UIInputEventType : byte
    {
        Unknown = 0,

        // Mouse
        /// <remarks>Fired on all interactables in queue</remarks>
        MouseMotion,

        /// <remarks>Fired on all interactables in queue</remarks>
        MouseWheel,

        /// <remarks>Only fired on first interactable in queue</remarks>
        MouseEnter,

        /// <remarks>Only fired on source interactable</remarks>
        MouseLeave,

        /// <remarks>Only fired on source interactable</remarks>
        MouseDown,

        /// <remarks>Only fired on source interactable</remarks>
        MouseUp,

        /// <remarks>Only fired on source interactable</remarks>
        MousePress,

        // Focus

        /// <remarks>Only fired on source interactable</remarks>
        FocusGained,

        /// <remarks>Only fired on source interactable</remarks>
        FocusLost,

        // Dragging

        /// <remarks>Only fired on interactable with button focus</remarks>
        DragBegin,

        /// <remarks>Only fired on interactable with button focus</remarks>
        DragUpdate,

        /// <remarks>Only fired on interactable with button focus</remarks>
        DragEnd,

        // Keyboard

        /// <remarks>Only fired on interactable with input focus</remarks>
        KeyDown,

        /// <remarks>Only fired on interactable with input focus</remarks>
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
