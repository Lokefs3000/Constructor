using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Layout;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    public class UILabel : UIElement
    {
        private UIFontStyle? _fontStyle;

        private string _text;
        private float _size;

        private float? _lineHeight;
        private float _letterSpacing;

        private UITextAlignment _alignment;
        private UITextOverflow _overflow;

        private UITextAutoSize _autoSize;

        private UIColor _fillColor;

        private UITextShapingData _shapingData;

        public UILabel()
        {
            _fontStyle = null;

            _text = string.Empty;
            _size = 16.0f;

            _lineHeight = null;
            _letterSpacing = 0.0f;

            _alignment = UITextAlignment.Left | UITextAlignment.Top;
            _overflow = UITextOverflow.Overflow;

            _autoSize = UITextAutoSize.None;

            _fillColor = Color.Black;

            _shapingData = new UITextShapingData();
        }

        public override void MeasureSize(UILayoutManager manager)
        {
            switch (_autoSize)
            {
                case UITextAutoSize.None:
                    {
                        base.MeasureSize(manager);
                        break;
                    }
                case UITextAutoSize.FitSizeToText:
                    {
                        if (Flags.HasFlag(InvalidFlags, (UIInvalidationFlags)InvalidFlagsExt.ShapingData))
                        {
                            if (_fontStyle != null)
                                RecalculateShapingData(manager);
                            RemoveInvalidFlag((UIInvalidationFlags)InvalidFlagsExt.ShapingData);
                        }

                        SetRealSize(_shapingData.TotalSize);
                        break;
                    }
                case UITextAutoSize.FitTextToSize: throw new NotImplementedException();
                default: throw new NotSupportedException();
            }
        }

        public override void RecalculateLayout(UILayoutManager manager, ref UIMeasurements measurements)
        {
            if (Flags.HasFlag(InvalidFlags, (UIInvalidationFlags)InvalidFlagsExt.ShapingData))
            {
                if (_fontStyle != null)
                    RecalculateShapingData(manager);
                RemoveInvalidFlag((UIInvalidationFlags)InvalidFlagsExt.ShapingData);
            }

            base.RecalculateLayout(manager, ref measurements);
        }

        public override bool DrawVisual(UICommandBuffer commandBuffer)
        {
            if (_fontStyle != null)
            {
                //commandBuffer.AddStroke(ZIndex, Transform.RenderCoordinates, new Color(1.0f, 0.0f, 1.0f), 1.0f);
                commandBuffer.AddText(ZIndex, Transform.RenderCoordinates, _fillColor, _shapingData, _size, _lineHeight.GetValueOrDefault(_fontStyle.LineHeight), _letterSpacing, _alignment);
            }
            return base.DrawVisual(commandBuffer);
        }

        private void RecalculateShapingData(UILayoutManager manager)
        {
            manager.TextShaper.ShapeText(_shapingData, new UITextShapingInfo
            {
                Style = _fontStyle!,

                Text = _text,
                Size = _size,

                LetterSpacing = _letterSpacing,
                LineHeight = _lineHeight.GetValueOrDefault(_fontStyle!.LineHeight * _size),
                Overflow = _overflow,

                MaxExtents = (_autoSize == UITextAutoSize.FitSizeToText) ? Vector2.PositiveInfinity : Transform.RenderCoordinates.Size
            });

            InvalidateSelf(UIInvalidationFlags.Visual);
        }

        public UIFontStyle? FontStyle { get => _fontStyle; set { _fontStyle = value; InvalidateSelf(UIInvalidationFlags.All | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }

        public string Text { get => _text; set { _text = value; InvalidateSelf(UIInvalidationFlags.All | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }
        public float Size { get => _size; set { _size = value; InvalidateSelf(UIInvalidationFlags.All | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }

        public float? LineHeight { get => _lineHeight; set { _lineHeight = value; InvalidateSelf(UIInvalidationFlags.All | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }
        public float LetterSpacing { get => _letterSpacing; set { _letterSpacing = value; InvalidateSelf(UIInvalidationFlags.All | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }

        public UITextAlignment Alignment { get => _alignment; set { _alignment = value; InvalidateSelf(UIInvalidationFlags.All | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }
        public UITextOverflow Overflow { get => _overflow; set { _overflow = value; InvalidateSelf(UIInvalidationFlags.All | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }

        public UITextAutoSize AutoSize { get => _autoSize; set { _autoSize = value; InvalidateSelf(UIInvalidationFlags.Layout | (UIInvalidationFlags)InvalidFlagsExt.ShapingData); } }

        public UIColor FillColor { get => _fillColor; set { _fillColor = value; InvalidateSelf(UIInvalidationFlags.Visual); } }

        public UITextShapingData ShapingData => _shapingData;

        private enum InvalidFlagsExt : byte
        {
            ShapingData = 1 << 4
        }
    }

    public enum UITextAlignment : byte
    {
        Left = 0b000_001,
        Center = 0b000_010,
        Right = 0b000_011,

        Top = 0b001_000,
        Middle = 0b010_000,
        Bottom = 0b011_000,

        LCRMask = 0b000_011,
        TMBMask = 0b011_000
    }

    public enum UITextOverflow : byte
    {
        Overflow = 0,
        Cull,
        WrapWords,
        WrapLetters
    }

    public enum UITextAutoSize : byte
    {
        None = 0,
        FitSizeToText = 1 << 0,
        FitTextToSize = 1 << 1,
    }
}
