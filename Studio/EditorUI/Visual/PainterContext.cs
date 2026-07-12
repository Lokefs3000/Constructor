using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Assets;
using EditorUI.Built;
using EditorUI.Text;
using EditorUI.Utility;
using EditorUI.Visual.Built;
using Primary.Assets;
using Primary.Mathematics;
using Primary.RHI;

namespace EditorUI.Visual
{
    public record struct PainterContext
    {
        private readonly Painter _data;
        private int _translateStackSize;
        private int _clipStackStack;

        internal PainterContext(Painter data)
        {
            _data = data;

            _translateStackSize = 0;
            _clipStackStack = 0;
        }

        public readonly void AddPoint(Vector2 point, Paint paint, float radius = 1.0f)
        {
            _data.AddPoints(new ReadOnlySpan<Vector2>(ref point), paint, radius);
        }

        public readonly void AddPoints(ReadOnlySpan<Vector2> points, Paint paint, float radius = 1.0f)
        {
            _data.AddPoints(points, paint, radius);
        }

        public readonly void AddLine(Vector2 from, Vector2 to, Paint paint, float thickness = 1.0f)
        {
            Span<Vector2> temp = [from, to];
            _data.AddLines(temp, paint, LinePaintMode.List, thickness);
        }

        public readonly void AddLines(ReadOnlySpan<Vector2> points, Paint paint, LinePaintMode paintMode = LinePaintMode.List, float thickness = 1.0f)
        {
            _data.AddLines(points, paint, paintMode, thickness);
        }

        public readonly void AddRectangle(Boundaries boundaries, Paint paint)
        {
            _data.AddRectangle(boundaries, paint, Vector4.NegativeZero);
        }

        public readonly void AddRectangle(Boundaries boundaries, Paint paint, Vector4 cornerRadius)
        {
            _data.AddRectangle(boundaries, paint, cornerRadius);
        }

        public readonly void AddQuad(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br, Paint paint)
        {
            _data.AddQuad(tl, tr, bl, br, paint);
        }

        public readonly void AddImage(Boundaries boundaries, TextureAsset image, Boundaries uvs, Paint paint)
        {
            _data.AddImage(boundaries, uvs, paint, image);
        }

        public readonly void AddImage(Boundaries boundaries, RHITexture image, Boundaries uvs, Paint paint)
        {
            _data.AddImage(boundaries, uvs, paint, image);
        }

        public readonly void AddImage(Boundaries boundaries, Sprite? image, Paint paint)
        {
#if DEBUG
            if (image == null)
            {
                _data.AddImage(boundaries, new Boundaries(Vector2.Zero, Vector2.One), paint, AssetManager.Static.DebugTexError);
                return;
            }
#else
            if (image != null)
#endif
            _data.AddImage(boundaries, new Boundaries(image.UVMin, image.UVMax), paint, image.Texture);
        }

        public readonly void AddCircle(Vector2 center, float radius, Paint paint)
        {
            _data.AddCircle(center, radius, paint);
        }

        public readonly void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Paint paint, float cornerRadius = -1.0f)
        {
        }

        public readonly void AddText(Vector2 position, TextShapingData shapingData, Vector2 maxExtents, Paint paint)
        {
            _data.AddText(position, shapingData, paint, maxExtents);
        }

        public void PushClippingRect(Rect rect)
        {
            _data.PushClip(rect);
            ++_clipStackStack;
        }

        public void PopClippingRect()
        {
            if (_clipStackStack > 0)
            {
                _data.PopClip();
                --_clipStackStack;
            }
        }

        public void PushTranslate(Vector2 translation)
        {
            _data.PushTranslate(translation);
            ++_translateStackSize;
        }

        public void PopTranslate()
        {
            if (_translateStackSize > 0)
            {
                _data.PopTranslate();
                --_translateStackSize;
            }
        }

        internal readonly int TranslateStackSize => _translateStackSize;
        internal readonly int ClipStackSize => _clipStackStack;
    }

    public enum LinePaintMode : byte
    {
        /// <summary>v0 -> v1; v2 -> v3; v4 -> v5; ...</summary>
        List = 0,
        /// <summary>v0 -> v1; v1 -> v2; v2 -> v3; ...</summary>
        Strip
    }
}
