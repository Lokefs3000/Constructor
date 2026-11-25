using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Layout
{
    public readonly record struct UIMeasureContext(UILayoutManager Manager, Vector2 LocalRegion);
    public readonly record struct UIModMeasureContext(UILayoutManager Manager, Vector2 LocalRegion, Vector2 TreeSize);
}
