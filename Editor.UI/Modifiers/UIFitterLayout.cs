using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Text;

namespace Editor.UI.Modifiers
{
    [ModifierPrettyName("FitterLayout")]
    public class UIFitterLayout : BaseLayoutModifier
    {
        private UIFitterAxis _axis;
        private UIValue2 _margin;

        public UIFitterLayout(UIElement element) : base(element)
        {
            _axis = UIFitterAxis.None;
            _margin = UIValue2.Zero;
        }

        public override void ModifyLayout(UILayoutContext context)
        {
            Vector2 calc = _margin.Evaluate(_element.CurrentSize);
            Vector2 calc2x = calc + calc;

            switch (_axis)
            {
                case UIFitterAxis.Vertical: _element.CurrentSize = new Vector2(_element.CurrentSize.X, context.Measurements.TreeSize.Y + calc2x.Y); break;
                case UIFitterAxis.Horizontal: _element.CurrentSize = new Vector2(context.Measurements.TreeSize.X + calc2x.X, _element.CurrentSize.Y); break;
                case UIFitterAxis.Both: _element.CurrentSize = context.Measurements.TreeSize + calc2x; break;
            }

            foreach (UIElement element in _element.Children)
            {
                if (Flags.HasFlag(element.StateFlags, UIStateFlags.InvalidLayout))
                    element.RelativeOffset += calc;
            }

            context.Measurements.MarkAsOutdated();
        }

        #region Properties
        [EditableProperty(nameof(_axis))] public UIFitterAxis Axis { get => _axis; set { _axis = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }
        [EditableProperty(nameof(_margin))] public UIValue2 Margin { get => _margin; set { _margin = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }
        #endregion
    }

    public enum UIFitterAxis : byte
    {
        None = 0,

        Vertical = 1 << 0,
        Horizontal = 1 << 1,

        Both = Vertical | Horizontal
    }
}
