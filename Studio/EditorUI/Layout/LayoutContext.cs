using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditorUI.Layout
{
    public readonly record struct LayoutContext(Vector2 ParentSize, Vector2 ParentOffset, LayoutLockAxis LayoutLock);

    public enum LayoutLockAxis : byte
    {
        None = 0,

        AxisX = 1 << 0,
        AxisY = 1 << 1,

        AxisXY = AxisX | AxisY
    }
}
