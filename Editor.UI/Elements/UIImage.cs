using Editor.UI.Datatypes;
using Editor.UI.Visual;
using Primary.Assets;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("Image")]
    public class UIImage : UIElement
    {
        private TextureAsset? _image;

        private Vector2 _uvMin;
        private Vector2 _uvMax;

        private UIColor _tintColor;

        public UIImage()
        {
            _image = null;

            _uvMin = Vector2.Zero;
            _uvMax = Vector2.One;

            _tintColor = Color.White;
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            if (_image != null)
            {
                painter.DrawImage(PixelCoordinates, UIPaint.FromColor(_tintColor), _uvMin, _uvMax, _image);
            }

            return true;
        }

        #region Properties
        [StyleableProperty(nameof(_image), UIStateFlags.InvalidVisual)] public TextureAsset? Image { get => _image; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_uvMin), UIStateFlags.InvalidVisual)] public Vector2 UVMin { get => _uvMin; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_uvMax), UIStateFlags.InvalidVisual)] public Vector2 UVMax { get => _uvMax; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_tintColor), UIStateFlags.InvalidVisual)] public UIColor TintColor { get => _tintColor; set => SetStyleProperty(value); }
        #endregion
    }
}
