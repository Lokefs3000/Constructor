using Editor.UI.Visual;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Menu
{
    public sealed class ContextMenuSeparator : ContextMenuBase
    {
        public override Vector2 MeasureSize()
        {
            return new Vector2(10.0f, 2.0f);
        }

        public override void DrawVisual(Vector2 basePosition, Vector2 availRegion, UIPainterContext painter)
        {
            painter.DrawRect(new Boundaries(new Vector2(basePosition.X + 2.0f, basePosition.Y), new Vector2(basePosition.X + availRegion.X, basePosition.Y) + new Vector2(-4.0f, 2.0f)), UIPaint.FromColor(s_separatorColor));
        }

        private static readonly Color s_separatorColor = Color.FromHex("101010");
    }
}
