using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Menu
{
    public class ContextMenuItem : ContextMenuBase
    {
        private List<ContextMenuBase>? _items;

        private UIFontAsset? _font;
        private FontStyle _fontStyle;
        private FontWeight _fontWeight;

        private string? _text;
        private object? _image;

        public ContextMenuItem()
        {
            _items = null;

            _font = null;
            _fontStyle = FontStyle.Normal;
            _fontWeight = FontWeight._300;

            _text = GetType().Name;
            _image = null;
        }

        public override void SetOwner(ContextMenuHost? newHost)
        {
            if (_host == newHost)
                return;

            _host = newHost;

            if (_items != null)
            {
                foreach (ContextMenuBase item in _items)
                {
                    item.SetOwner(newHost);
                }
            }
        }

        protected override bool AddChild(ContextMenuBase item)
        {
            (_items ??= []).Add(item);
            return true;
        }

        protected override void RemoveChild(ContextMenuBase item)
        {
            _items?.Remove(item);
        }

        public override Vector2 MeasureSize()
        {
            float totalSize = 0.0f;
            if (_image != null)
            {
                totalSize += TextManager.PixelsPerEM + 4.0f;
            }

            if (string.IsNullOrEmpty(_text))
                return new Vector2(totalSize, TextManager.PixelsPerEM);

            UIFontTypeData? fontStyle = _font?.FindStyle(_fontStyle, _fontWeight);
            if (fontStyle == null)
                return new Vector2(totalSize, TextManager.PixelsPerEM);

            TextVisualInfo visualInfo = new TextVisualInfo(new PaintColor(Color.White), 1.0f, fontStyle);
            TextWrapInfo wrapInfo = new TextWrapInfo(TextOrigin.Top, Vector2.PositiveInfinity, true, visualInfo);

            TextManager text = UIManager.Instance.TextManager;

            StringHandle stringHandle = text.GetStringHandle(_text);
            ShapedTextData textData = text.ShapeText(wrapInfo, UITextOverflow.Overflow, stringHandle.String, stringHandle.Hash);

            return new Vector2(totalSize + textData.TotalSize.X + 10.0f, TextManager.PixelsPerEM);
        }

        public override void DrawVisual(Vector2 basePosition, Vector2 availRegion, UIPainterContext painter)
        {
            if (string.IsNullOrEmpty(_text))
                return;

            UIFontTypeData? fontStyle = _font?.FindStyle(_fontStyle, _fontWeight);
            if (fontStyle == null)
                return;

            if (_image != null)
            {
                if (_image is TextureAsset texture)
                    painter.DrawImage(new Boundaries(basePosition, basePosition + new Vector2(TextManager.PixelsPerEM)), UIPaint.FromColor(Color.White), texture);
                else if (_image is RHITexture rhiTexture)
                    painter.DrawImage(new Boundaries(basePosition, basePosition + new Vector2(TextManager.PixelsPerEM)), UIPaint.FromColor(Color.White), rhiTexture);
                else if (_image is Sprite sprite)
                    painter.DrawImage(new Boundaries(basePosition, basePosition + new Vector2(TextManager.PixelsPerEM)), UIPaint.FromColor(Color.White), sprite);
                else
                    painter.DrawRect(new Boundaries(basePosition, basePosition + new Vector2(TextManager.PixelsPerEM)), UIPaint.FromColor(Color.Pink));

                basePosition.X += TextManager.PixelsPerEM + 4.0f;
            }

            painter.DrawText(new Vector2(basePosition.X, basePosition.Y + TextManager.PixelsPerEM - 2.0f), UIPaint.FromColor(Color.White), new TextBuilder().SetOrigin(TextOrigin.Bottom), fontStyle, 1.0f, _text);
        }

        public ROList<ContextMenuBase> Items => _items ?? ROList<ContextMenuBase>.Empty;

        #region Properties
        public UIFontAsset? Font { get => _font; set => _font = value; }
        public FontStyle FontStyle { get => _fontStyle; set => _fontStyle = value; }
        public FontWeight FontWeight { get => _fontWeight; set => _fontWeight = value; }

        public string? Text { get => _text; set => _text = value; }

        public TextureAsset? Image { get => _image as TextureAsset; set => _image = value; }
        public RHITexture? RHITexture { get => _image as RHITexture; set => _image = value; }
        public Sprite? Sprite { get => _image as Sprite; set => _image = value; }
        #endregion
    }
}
