using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Editor.Gui.Events.UIEvent;

namespace Editor.Gui.Events
{
    [StructLayout(LayoutKind.Explicit)]
    public record struct UIEvent
    {
        [FieldOffset(0)]
        public UIEventType Type;

        [FieldOffset(1)]
        public uint WindowId;

        [FieldOffset(5)]
        public Vector2 MouseHit;

        [FieldOffset(13)]
        public MouseData Mouse;

        public record struct MouseData(Vector2 Delta, UIMouseButton Button);
    }

    public enum UIEventType : byte
    {
        MouseMotion,
        MouseButtonUp,
        MouseButtonDown,
        MouseWheel,
        MouseLeave,
        MouseEnter
    }

    public enum UIMouseButton : byte
    {
        None = 0,

        Left = 1 << 0,
        Middle = 1 << 1,
        Right = 1 << 2,
    }
}
