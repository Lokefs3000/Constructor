using Primary.Input.Devices;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Interaction
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UIEvent
    {
        [FieldOffset(0)]
        public UIEventType Type;

        [FieldOffset(1)]
        public UIMouseEvent Mouse;

        internal UIEvent(UIEventType type, UIMouseEvent data)
        {
            Type = type;
            Mouse = data;
        }
    }

    public enum UIEventType : byte
    {
        None = 0,

        //Mouse
        MouseMotion,
        MouseWheel,
        MouseEnter,
        MouseLeave,
        MouseActivate,
        MouseDeactivate
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
}
