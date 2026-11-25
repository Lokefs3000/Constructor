using Editor.UI.Datatypes;
using Editor.UI.Visual;
using Primary.Assets;
using Primary.Common;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("RawImage")]
    public class UIRawImage : UIElement
    {
        private RHITexture? _image;

        private Vector2 _uvMin;
        private Vector2 _uvMax;

        private UIColor _tintColor;

        public UIRawImage()
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
                painter.DrawImage(_viewCoordinates, UIPaint.FromColor(_tintColor), _uvMin, _uvMax, _image);
            }

            return true;
        }

        public RHITexture? Image { get => _image; set { _image = value; AddStateFlags(UIStateFlags.InvalidVisual); } }

        #region Properties
        [StyleableProperty(nameof(_uvMin), UIStateFlags.InvalidVisual)] public Vector2 UVMin { get => _uvMin; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_uvMax), UIStateFlags.InvalidVisual)] public Vector2 UVMax { get => _uvMax; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_tintColor), UIStateFlags.InvalidVisual)] public UIColor TintColor { get => _tintColor; set => SetStyleProperty(value); }
        #endregion
    }
}
