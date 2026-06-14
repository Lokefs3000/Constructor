using Editor.UI.Datatypes;
using Primary.Assets;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.UI.Visual
{
    public struct UIPaint
    {
        private UIColor _color;

        private bool _strokeEnabled;
        private float _strokeWidth;
        private UIColor _strokeColor;

        private ShaderAsset? _customShader;

        public UIPaint()
        {
            _color = Color.TransparentWhite;

            _strokeEnabled = false;
            _strokeWidth = 1.0f;
            _strokeColor = Color.Black;

            _customShader = null;
        }

        public UIPaint Clear()
        {
            this = new UIPaint();
            return this;
        }

        public UIPaint SetColor(UIColor color)
        {
            _color = color;
            return this;
        }

        public UIPaint SetStroke(bool enabled)
        {
            _strokeEnabled = enabled;
            return this;
        }

        public UIPaint SetStrokeWidth(float width)
        {
            _strokeWidth = width;
            return this;
        }

        public UIPaint SetStrokeColor(UIColor color)
        {
            _strokeColor = color;
            return this;
        }

        public UIPaint SetShader(ShaderAsset? shader)
        {
            _customShader = shader;
            return this;
        }

        internal RawPaintData ToRaw(UIPainter painter)
        {
            return new RawPaintData(
                new PaintColor(_color.Solid),
                _strokeEnabled,
                _strokeWidth,
                new PaintColor(_strokeColor.Solid),
                _customShader == null ? int.MinValue : painter.GetObjectIndex(_customShader));
        }

        public static UIPaint FromColor(UIColor color) => new UIPaint().SetColor(color);
    }

    public enum StrokePosition : byte
    {
        Inside = 0,
        Middle,
        Outside
    }
}
