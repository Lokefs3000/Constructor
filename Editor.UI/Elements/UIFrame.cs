using Editor.UI.Datatypes;
using Editor.UI.Reflection;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("Frame")]
    public class UIFrame : UIElement
    {
        protected float _cornerRadius;
        protected RectCorner _corners;

        protected UIColor _backgroundColor;

        protected UIColor _strokeColor;
        protected StrokePosition _strokePosition;
        protected float _strokeWeight;

        public UIFrame()
        {
            _cornerRadius = 0.0f;
            _corners = RectCorner.All;

            _backgroundColor = Color.White;

            _strokeColor = Color.Black;
            _strokePosition = StrokePosition.Inside;
            _strokeWeight = 0.0f;
        }

        public UIFrame(UIElement parent) : this()
        {
            SetParent(parent);
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            UIPaint paint = UIPaint.FromColor(_backgroundColor);
            if (_strokeWeight > 0.0f)
            {
                paint.SetStroke(true);
                paint.SetStrokeWidth(_strokeWeight);
                paint.SetStrokeColor(_strokeColor);
            }

            painter.DrawRect(PixelCoordinates, paint, _cornerRadius);
            return base.DrawVisual(painter);
        }

        #region Styleable
        [StyleableProperty(nameof(_backgroundColor), UIStateFlags.InvalidVisual)] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_cornerRadius), UIStateFlags.InvalidVisual)] public float CornerRadius { get => _cornerRadius; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_corners), UIStateFlags.InvalidVisual)] public RectCorner Corners { get => _corners; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_strokeColor), UIStateFlags.InvalidVisual)] public UIColor StrokeColor { get => _strokeColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_strokePosition), UIStateFlags.InvalidVisual)] public StrokePosition StrokePosition { get => _strokePosition; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_strokeWeight), UIStateFlags.InvalidVisual)] public float StrokeWeight { get => _strokeWeight; set => SetStyleProperty(value); }
        #endregion
    }
}
