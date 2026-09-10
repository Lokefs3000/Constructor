using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Assets;
using EditorUI.Common;
using EditorUI.Layout;
using EditorUI.Visual;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.RHI;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class Image : Widget
    {
        [StyleSetup(ConverterTypes = [typeof(Sprite), typeof(RHITexture), typeof(TextureAsset)])] protected object? _image;

        [StyleInclude] protected Vector2 _uvMin;
        [StyleInclude] protected Vector2 _uvMax;

        [StyleInclude] protected UIColor _imageTint;

        public Image()
        {
            _image = null;

            _uvMin = Vector2.Zero;
            _uvMax = Vector2.One;

            _imageTint = Color.White;
        }

        protected internal override void PaintSelf(ref PainterContext context)
        {
            if (_image != null)
            {
                if (_image is Sprite sprite)
                    context.AddImage(_computedRect, sprite, new Paint(_imageTint));
                else if (_image is TextureAsset texture)
                    context.AddImage(_computedRect, texture, new Boundaries(_uvMin, _uvMax), new Paint(_imageTint));
                else if (_image is RHITexture rhi)
                    context.AddImage(_computedRect, rhi, new Boundaries(_uvMin, _uvMax), new Paint(_imageTint));
                else
                    context.AddImage(_computedRect, AssetManager.Static.DebugTexError, new Boundaries(_uvMin, _uvMax), new Paint(_imageTint));
            }
        }

        #region Serialization
        public Sprite? Sprite { get => _image as Sprite; set => SetStyledField(value); }
        public RHITexture? RHITexture { get => _image as RHITexture; set => SetStyledField(value); }
        public TextureAsset? Texture { get => _image as TextureAsset; set => SetStyledField(value); }

        public Vector2 UVMinimum { get => _uvMin; set => SetStyledField(value); }
        public Vector2 UVMaximum { get => _uvMax; set => SetStyledField(value); }
        #endregion
    }

    public enum AspectSource : byte
    {
        None = 0,
        X,
        Y,
    }
}
