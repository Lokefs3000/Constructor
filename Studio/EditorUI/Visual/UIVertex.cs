using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Common;

namespace EditorUI.Visual
{
    public readonly record struct UIVertex(Vector2 Position, Vector2 UV, Color Tint, uint DataOffset);
}
