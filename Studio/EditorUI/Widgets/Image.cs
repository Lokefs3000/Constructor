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
        protected AspectSource _aspectSource;
        protected float _aspectRatio;

        protected object? _image;

        protected Vector2 _uvMin;
        protected Vector2 _uvMax;

        protected UIColor _imageTint;

        public Image()
        {
            _aspectSource = AspectSource.None;
            _aspectRatio = 1.0f;

            _image = null;

            _uvMin = Vector2.Zero;
            _uvMax = Vector2.One;

            _imageTint = Color.White;
        }

        protected internal override MeasureStatus MeasureSelf(ref readonly LayoutContext context)
        {
            base.MeasureSelf(in context);

            switch (_aspectSource)
            {
                case AspectSource.X:
                    {
                        float aspectY = _layoutState.IdealSize.X * _aspectRatio;
                        _layoutState.ContentSize.Y += aspectY - _layoutState.IdealSize.Y;
                        _layoutState.IdealSize.Y = aspectY;
                        break;
                    }
                case AspectSource.Y:
                    {
                        float aspectX = _layoutState.IdealSize.Y * _aspectRatio;
                        _layoutState.ContentSize.X += aspectX - _layoutState.IdealSize.X;
                        _layoutState.IdealSize.X = aspectX;
                        break;
                    }
            }

            return MeasureStatus.Success;
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
        [Styled(nameof(_aspectSource), StateFlags.SelfInvalidLayout)] public AspectSource AspectSource { get => _aspectSource; set => SetStyledField(value); }
        [Styled(nameof(_aspectRatio), StateFlags.SelfInvalidLayout)] public float AspectRatio { get => _aspectRatio; set => SetStyledField(value); }

        [Styled(nameof(_image)), StyleConverterTypes(typeof(TextureAsset), typeof(Sprite))]
        public object? Picture
        {
            get => _image;
            set
            {
                if (value is not Sprite and not TextureAsset and not RHITexture)
                    SetStyledField<object?>(null);
                else
                    SetStyledField(value);
            }
        }

        [Styled(nameof(_uvMin))] public Vector2 UVMinimum { get => _uvMin; set => SetStyledField(value); }
        [Styled(nameof(_uvMax))] public Vector2 UVMaximum { get => _uvMax; set => SetStyledField(value); }
        #endregion
    }

    public enum AspectSource : byte
    {
        None = 0,
        X,
        Y,
    }
}
