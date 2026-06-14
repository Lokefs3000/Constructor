using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditorUI.Layout
{
    public readonly record struct LayoutContext(Vector2 ParentSize, Vector2 ParentOffset, bool IsLayoutLocked);
}
