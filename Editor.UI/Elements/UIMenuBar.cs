using Editor.UI.Datatypes;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("MenuBar")]
    public class UIMenuBar : UIElement
    {
        protected UIColor _backgroundColor;

        protected UIColor _strokeColor;
        protected float _strokeWeight;

        public UIMenuBar()
        {
            _backgroundColor = Color.White;

            _strokeColor = Color.Black;
            _strokeWeight = 0.0f;
        }

        public override void RecalculateLayout(UILayoutContext context)
        {
            float currentOffset = ItemPadding;

            

            _viewOffset = Vector2.Zero;
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            painter.DrawRect(ViewCoordinates, UIPaint.FromColor(_backgroundColor));

            if (_strokeWeight > 0.0f)
            {
                painter.DrawRect(new Boundaries(new Vector2(ViewCoordinates.Minimum.X, ViewCoordinates.Maximum.Y - _strokeWeight), ViewCoordinates.Maximum), UIPaint.FromColor(_strokeColor));
            }

            return base.DrawVisual(painter);
        }

        #region Properties
        [StyleableProperty(nameof(_backgroundColor), UIStateFlags.InvalidVisual)] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_strokeColor), UIStateFlags.InvalidVisual)] public UIColor StrokeColor { get => _strokeColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_strokeWeight), UIStateFlags.InvalidVisual)] public float StrokeWeight { get => _strokeWeight; set => SetStyleProperty(value); }
        #endregion

        public const float ItemPadding = 4.0f;
    }
}
