using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Modifiers
{
    public interface IUILayoutModifier
    {
        public void MeasureSize(UIMeasureContext context);
        public void ModifyElement(UILayoutContext context);
        public void DrawVisual(UIPainterContext painter);
    }
}
