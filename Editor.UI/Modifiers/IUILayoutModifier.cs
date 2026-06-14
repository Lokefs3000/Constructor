using Editor.UI.Elements;
using Editor.UI.Interaction;
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
        public UIElement Owner { get; }

        public void ModifySize(UIMeasureContext context);
        public void ModifyLayout(UILayoutContext context);
        public void ModifyVisual(UIPainterContext painter);
    }
}
