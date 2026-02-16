using Editor.UI.Datatypes;
using Editor.UI.Visual;
using Primary.Assets;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    public class UIImage : UIElement
    {
        private TextureAsset? _image;

        private UIColor _tintColor;

        public UIImage()
        {
            _image = null;

            _tintColor = Color.White;
        }

        public override bool DrawVisual(UICommandBuffer commandBuffer)
        {
            if (_image != null)
            {
                commandBuffer.AddImage(ZIndex, Transform.RenderCoordinates, _tintColor, _image);
            }

            return base.DrawVisual(commandBuffer);
        }

        public TextureAsset? Image { get => _image; set { _image = value; InvalidateSelf(UIInvalidationFlags.Visual); } }

        public UIColor TintColor { get => _tintColor; set { _tintColor = value; InvalidateSelf(UIInvalidationFlags.Visual); } }
    }
}
