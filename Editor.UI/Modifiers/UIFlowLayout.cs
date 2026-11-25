using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Modifiers
{
    [ModifierPrettyName("FlowLayout")]
    public class UIFlowLayout : BaseLayoutModifier
    {
        private UIValue2 _padding;

        public UIFlowLayout(UIElement element) : base(element)
        {
            _padding = UIValue2.Zero;
        }

        public override void ModifyLayout(UILayoutContext context)
        {
            Vector2 padding = _padding.Evaluate(context.LocalRegion);
            Vector2 position = Vector2.Zero;

            Vector2 extents = _element.CurrentSize;
            float maxHeight = 0.0f;

            foreach (UIElement child in _element.Children)
            {
                Vector2 childExtents = child.CurrentSize;
                childExtents.X += padding.X;

                if (position.X + childExtents.X > extents.X)
                {
                    position.X = 0.0f;
                    position.Y += maxHeight + padding.Y;
                }

                child.RelativeOffset = position;

                position.X += childExtents.X;
                maxHeight = MathF.Max(maxHeight, childExtents.Y);
            }
        }

        #region Properties
        [EditableProperty(nameof(_padding))] public UIValue2 Padding { get => _padding; set { _padding = value; _element.AddStateFlags(UIStateFlags.InvalidLayout); } }
        #endregion
    }
}
