using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Modifiers
{
    public sealed class UIMenuBar : IUILayoutModifier
    {
        private readonly UIElement _element;

        private UIColor _fillColor;
        private UIColor _strokeColor;

        private float _strokeWeight;

        public UIMenuBar(UIElement element)
        {
            _element = element;

            _fillColor = Color.White;
            _strokeColor = new Color(0.5f);

            _strokeWeight = 1.0f;
        }

        public void MeasureSize(Vector2 treeSize)
        {
            //not implemented
        }

        public void ModifyElement(ref UIMeasurements measurements)
        {
            float height = MenuBarHeight + _strokeWeight;
            foreach (UIElement element in _element.Children)
            {
                element.Transform.RelativePosition =
                    new Vector2(element.Transform.RelativePosition.X, element.Transform.RelativePosition.Y + height);
            }

            measurements.MarkAsOutdated();
        }

        public void DrawVisual(UICommandBuffer commandBuffer)
        {
            Boundaries parentRenderCoords = _element.Transform.RenderCoordinates;
            Boundaries boundaries = new Boundaries(parentRenderCoords.Minimum, new Vector2(parentRenderCoords.Maximum.X, parentRenderCoords.Minimum.Y + MenuBarHeight));

            commandBuffer.AddRectangle(_element.ZIndex, boundaries, _fillColor);
            if (_strokeWeight > 0.0f)
                commandBuffer.AddRectangle(_element.ZIndex, new Boundaries(new Vector2(boundaries.Minimum.X, boundaries.Maximum.Y), new Vector2(boundaries.Maximum.X, boundaries.Maximum.Y + _strokeWeight + 0.5f)), _strokeColor);
        }

        public UIColor FillColor { get => _fillColor; set { _fillColor = value; _element.InvalidateSelf(UIInvalidationFlags.Visual); } }
        public UIColor StrokeColor { get => _strokeColor; set { _strokeColor = value; _element.InvalidateSelf(UIInvalidationFlags.Visual); } }

        public float StrokeWeight { get => _strokeWeight; set { _strokeWeight = MathF.Max(value, 0.0f); _element.InvalidateSelf(UIInvalidationFlags.All); } }

        private const float MenuBarHeight = 24.0f;
    }
}
