using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Menu
{
    public class ContextMenuItem : ContextMenuBase
    {
        private List<ContextMenuBase> _children;

        private string _text;

        public ContextMenuItem()
        {
            _children = new List<ContextMenuBase>();

            _text = "Menu item";
        }

        internal override void ChangeOwner(ContextMenuAsset? newOwner)
        {
            if (_owner != newOwner)
            {
                _owner = newOwner;

                foreach (ContextMenuBase child in _children)
                {
                    child.ChangeOwner(newOwner);
                }
            }
        }

        public override Vector2 MeasureSize()
        {
            if (string.IsNullOrEmpty(_text))
                return Vector2.Zero;

            if (Owner == null)
                return Vector2.Zero;

            UIFontStyle? fontStyle = Owner.Style;
            if (fontStyle == null)
                return Vector2.Zero;

            TextVisualInfo visualInfo = new TextVisualInfo(new PaintColor(Color.White), 1.0f, fontStyle);
            TextWrapInfo wrapInfo = new TextWrapInfo(Vector2.PositiveInfinity, visualInfo);

            using RentedArray<char> tempText = RentedArray<char>.Rent(_text.Length + 1);

            _text.CopyTo(tempText.Span);
            tempText.Span[_text.Length] = '\0';

            UIManager manager = UIManager.Instance;
            ShapedTextData textData = manager.TextManager.ShapeText(wrapInfo, UITextOverflow.Overflow, tempText.Span, _text.Length < 200 ? _text.GetDjb2HashCode() : StringHandle.InvalidHashCode);

            return textData.TotalSize;
        }

        public override void DrawVisual(Vector2 basePosition, Vector2 availRegion, UIPainterContext painter)
        {
            if (string.IsNullOrEmpty(_text))
                return;

            if (Owner == null)
                return;

            UIFontStyle? fontStyle = Owner.Style;
            if (fontStyle == null)
                return;

            painter.DrawText(basePosition, UIPaint.FromColor(Color.White), TextBuilder.Default, fontStyle, 1.0f, _text);
        }
    }
}
