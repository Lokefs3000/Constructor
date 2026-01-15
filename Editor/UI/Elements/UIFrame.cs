using Editor.UI.Datatypes;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    public class UIFrame : UIElement
    {
        private float _cornerRadius;
        private UIRoundedCorner _cornerRounding;

        private UIColor _fillColor;

        private UIColor _strokeColor;
        private UIStrokePosition _strokePosition;
        private float _strokeWeight;

        public UIFrame()
        {
            _cornerRadius = 0.0f;
            _cornerRounding = UIRoundedCorner.All;

            _fillColor = Color.White;

            _strokeColor = Color.White;
            _strokePosition = UIStrokePosition.Inside;
            _strokeWeight = 0.0f;
        }

        public override bool DrawVisual(UICommandBuffer commandBuffer)
        {
            commandBuffer.AddRectangle(ZIndex, Transform.RenderCoordinates, _fillColor, _cornerRadius, _cornerRounding);
            if (_strokeWeight > 0.0f)
                commandBuffer.AddBorder(ZIndex, Transform.RenderCoordinates, _strokeColor, _strokePosition, _strokeWeight, _cornerRadius, _cornerRounding);

            return true;
        }

        public float CornerRadius { get => _cornerRadius; set { _cornerRadius = value; InvalidateSelf(UIInvalidationFlags.Visual); } }
        public UIRoundedCorner CornerRounding { get => _cornerRounding; set { _cornerRounding = value; InvalidateSelf(UIInvalidationFlags.Visual); } }
        
        public UIColor FillColor { get => _fillColor; set { _fillColor = value; InvalidateSelf(UIInvalidationFlags.Visual); } }

        public UIColor StrokeColor { get => _strokeColor; set { _strokeColor = value; InvalidateSelf(UIInvalidationFlags.Visual); } }
        public UIStrokePosition StrokePosition { get => _strokePosition; set { _strokePosition = value; InvalidateSelf(UIInvalidationFlags.Visual); } }
        public float StrokeWeight { get => _strokeWeight; set { _strokeWeight = value; InvalidateSelf(UIInvalidationFlags.Visual); } }
    }
}
