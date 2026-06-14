using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Modifiers
{
    public abstract class BaseLayoutModifier : IUILayoutModifier
    {
        protected readonly UIElement _element;

        public BaseLayoutModifier(UIElement element)
        {
            _element = element;
        }

        public virtual void ModifySize(UIMeasureContext context) { }
        public virtual void ModifyLayout(UILayoutContext context) { }
        public virtual void ModifyVisual(UIPainterContext painter) { }

        public UIElement Owner => _element;
    }
}
