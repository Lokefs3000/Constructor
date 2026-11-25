using Primary.Input.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Interaction
{
    public readonly record struct UIEvent
    {
        public readonly UIEventType Type;
        public readonly UIInteractionManager Interactor;

        private readonly Union _union;

        internal UIEvent(UIEventType type, UIInteractionManager interactor, UIMouseEvent data)
        {
            Type = type;
            Interactor = interactor;
            _union = new Union(data);
        }

        internal UIEvent(UIEventType type, UIInteractionManager interactor, UIDragEvent data)
        {
            Type = type;
            Interactor = interactor;
            _union = new Union(data);
        }

        internal UIEvent(UIEventType type, UIInteractionManager interactor, UIKeyEvent data)
        {
            Type = type;
            Interactor = interactor;
            _union = new Union(data);
        }

        [UnscopedRef] public ref readonly UIMouseEvent Mouse => ref _union.Mouse;
        [UnscopedRef] public ref readonly UIDragEvent Drag => ref _union.Drag;
        [UnscopedRef] public ref readonly UIKeyEvent Key => ref _union.Key;

        [StructLayout(LayoutKind.Explicit)]
        private readonly record struct Union
        {
            [FieldOffset(0)] public readonly UIMouseEvent Mouse;
            [FieldOffset(0)] public readonly UIDragEvent Drag;
            [FieldOffset(0)] public readonly UIKeyEvent Key;

            public Union(UIMouseEvent data) { Mouse = data; }
            public Union(UIDragEvent data) { Drag = data; }
            public Union(UIKeyEvent data) { Key = data; }
        }
    }

    public enum UIEventType : byte
    {
        None = 0,

        // Mouse
        MouseMotion,
        MouseWheel,
        MouseEnter,
        MouseLeave,
        MouseDown,
        MouseUp,
        MouseActivate,
        MouseFocusLost,

        // Dragging
        DragBegin,
        DragUpdate,
        DragEnd,

        // Keyboard
        KeyDown,
        KeyUp,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UIMouseEvent
    {
        [FieldOffset(0)]
        public Vector2 Position;

        [FieldOffset(8)]
        public Vector2 Delta;

        [FieldOffset(8)]
        public MouseButton Button;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UIDragEvent
    {
        [FieldOffset(0)]
        public MouseButton Button;

        [FieldOffset(1)]
        public Vector2 Position;

        [FieldOffset(9)]
        public Vector2 Delta;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UIKeyEvent
    {
        [FieldOffset(0)]
        public KeyCode Key;

        [FieldOffset(1)]
        public bool IsRepeating;
    }
}
