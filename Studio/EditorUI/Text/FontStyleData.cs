using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using CommunityToolkit.HighPerformance;
using Editor.Interop.MSDF;
using EditorUI.Assets;
using EditorUI.Text.Visual;
using Primary.Common;
using TerraFX.Interop.Windows;

namespace EditorUI.Text
{
    public sealed class FontStyleData : IDisposable
    {
        private readonly FontFamily _family;
        private readonly FontStyle _style;
        private readonly FontWeight _weight;

        private readonly FontStyleAdvances _advances;
        private readonly FontStyleMetrics _metrics;

        private readonly FontGlyphAtlas _glyphAtlas;

        private Dictionary<char, FontGlyph> _glyphs;
        private HashSet<UnrenderedGlyph> _unrenderedGlyphs;

        private FontGlyph[] _asciiGlyphs;
        private readonly FontGlyph _invalidGlyph;

        private bool _disposedValue;

        internal FontStyleData(FontFamily family, FontStyle style, FontWeight weight, FontStyleAdvances advances, FontStyleMetrics metrics, IFontTextureFactory fontTextureFactory)
        {
            _family = family;
            _style = style;
            _weight = weight;

            _advances = advances;
            _metrics = metrics;

            _glyphAtlas = new FontGlyphAtlas(fontTextureFactory);

            _glyphs = new Dictionary<char, FontGlyph>();
            _unrenderedGlyphs = new HashSet<UnrenderedGlyph>();

            _asciiGlyphs = new FontGlyph[95];
            _invalidGlyph = RenderNewGlyph('\uffff');

            _glyphs.Add('\uffff', _invalidGlyph);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _glyphAtlas.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not thread-safe</summary>
        private unsafe FontGlyph RenderNewGlyph(char codepoint)
        {
            MSDF_ShapedGlyph* shapedGlyph = _family.GetGlyphContext(_style).ShapeNewGlyph(_weight, codepoint);
            if (shapedGlyph == null)
            {
                return _invalidGlyph;
            }

            MSDF_RenderBox renderBox;

            MSDFInterop.ScaleGlyph(shapedGlyph, _metrics.UnitsPerEm);
            MSDFInterop.CalculateBox(shapedGlyph, _family.Setup.MinScale, _family.Setup.PxRange, _family.Setup.MiterLimit, _family.Setup.PaddingX, _family.Setup.PaddingY, &renderBox);

            _unrenderedGlyphs.Add(new UnrenderedGlyph(codepoint, shapedGlyph, renderBox));

            double invBoxScale = 1.0 / _family.Setup.MinScale;
            double halfScale = invBoxScale * 0.5;

            Vector256<double> vec;
            {
                MSDF_Vector2 tmp = renderBox.Projection.Translate;

                Vector128<double> lower = -Vector128.LoadUnsafe(ref Unsafe.As<MSDF_Vector2, double>(ref tmp));
                Vector128<double> upper = lower + Vector128.Create((double)renderBox.RectW, renderBox.RectH) * Vector128.Create(invBoxScale);

                vec = Vector256.Create(lower, upper) + Vector256.Create(halfScale);
            }

            return new FontGlyph(
                new Vector4((float)vec[0], (float)-vec[3], (float)vec[2], (float)-vec[1]),
                new Vector2((float)(vec[2] - vec[0]), (float)(vec[1] - vec[3])),
                Vector4.Zero,
                (float)shapedGlyph->Advance);
        }

        /// <summary>Not thread-safe</summary>
        public ref readonly FontGlyph FindGlyph(char codepoint)
        {
            if (codepoint > AsciiMinValue && codepoint < AsciiMaxValue)
            {
                Debug.Assert(codepoint - AsciiMinValue < _asciiGlyphs.Length);

                ref FontGlyph glyph = ref _asciiGlyphs.DangerousGetReferenceAt(codepoint - AsciiMinValue);
                if (Unsafe.BitCast<float, uint>(glyph.Advance) == AsciiUnrendered)
                    glyph = RenderNewGlyph(codepoint);

                return ref glyph;
            }
            else
            {
                ref FontGlyph glyph = ref CollectionsMarshal.GetValueRefOrAddDefault(_glyphs, codepoint, out bool exists);
                if (!exists)
                    glyph = RenderNewGlyph(codepoint);

                return ref glyph;
            }
        }

        public FontFamily Family => _family;
        public FontStyle Style => _style;
        public FontWeight Weight => _weight;

        public FontStyleAdvances Advances => _advances;
        public FontStyleMetrics Metrics => _metrics;

        public FontGlyphAtlas GlyphAtlas => _glyphAtlas;

        private const ushort AsciiMinValue = 31;                // >
        private const ushort AsciiMaxValue = 127;               // <
        private const uint AsciiUnrendered = uint.MaxValue;     // ==
    }

    public readonly record struct FontStyleAdvances(short Space, short Tab);
    public readonly record struct FontStyleMetrics(float UnitsPerEm, short Ascender, short Descender, short LineHeight, short UnderlineY, short Height);

    internal readonly record struct UnrenderedGlyph(char Codepoint, Ptr<MSDF_ShapedGlyph> ShapedGlyph, MSDF_RenderBox RenderBox)
    {
        public override int GetHashCode() => Codepoint.GetHashCode();
    }

    internal readonly record struct AsciiGlyphData(FontGlyph Glyph, bool IsRendered);

    public readonly record struct FontGlyph(Vector4 PlaneBounds, Vector2 Dimensions, Vector4 UVBounds, float Advance);
}
