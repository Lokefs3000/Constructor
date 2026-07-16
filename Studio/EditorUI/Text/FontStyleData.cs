using System;
using System.Collections.Concurrent;
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
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;

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

        private readonly Lock _lock;

        private ConcurrentDictionary<char, FontGlyph> _glyphs;
        private ConcurrentDictionary<KerningKey, Vector2?> _kerning;

        private List<FontGlyphAtlasData> _glyphsInAtlas;
        private HashSet<UnrenderedGlyph> _unrenderedGlyphs;

        private FontGlyph[] _asciiGlyphs;
        private FontGlyph _invalidGlyph;

        private bool _hasRenderedInvalidGlyph;

        private bool _disposedValue;

        internal FontStyleData(FontFamily family, FontStyle style, FontWeight weight, FontStyleAdvances advances, FontStyleMetrics metrics, IFontTextureFactory fontTextureFactory)
        {
            _family = family;
            _style = style;
            _weight = weight;

            _advances = advances;
            _metrics = metrics;

            _glyphAtlas = new FontGlyphAtlas(fontTextureFactory, new Int2(family.Setup.PaddingX, family.Setup.PaddingY));

            _lock = new Lock();

            _glyphs = new ConcurrentDictionary<char, FontGlyph>();
            _kerning = new ConcurrentDictionary<KerningKey, Vector2?>();

            _glyphsInAtlas = new List<FontGlyphAtlasData>();
            _unrenderedGlyphs = new HashSet<UnrenderedGlyph>();

            _asciiGlyphs = new FontGlyph[95];
            _invalidGlyph = default;

            _hasRenderedInvalidGlyph = false;

            Array.Fill(_asciiGlyphs, new FontGlyph(default, default, default, default, float.PositiveInfinity, false));
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _glyphAtlas.Dispose();
                }

                if (_unrenderedGlyphs.Count > 0)
                {
                    unsafe
                    {
                        foreach (UnrenderedGlyph unrenderedGlyph in _unrenderedGlyphs)
                        {
                            MSDFInterop.DestroyShapedGlyph(unrenderedGlyph.ShapedGlyph.Pointer);
                        }
                    }
                }

                UIManager.Instance.FontRenderer.CancelFontStyleRender(this);

                _disposedValue = true;
            }
        }

        ~FontStyleData()
        {
            Dispose(disposing: false);
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
                if (!Volatile.Read(ref _hasRenderedInvalidGlyph))
                {
                    _hasRenderedInvalidGlyph = true;
                    _invalidGlyph = RenderNewGlyph('\uffff');

                    _glyphs['\uffff'] = _invalidGlyph;
                }

                return _invalidGlyph;
            }

            MSDF_RenderBox renderBox;

            MSDFInterop.ScaleGlyph(shapedGlyph, _metrics.UnitsPerEm);
            MSDFInterop.CalculateBox(shapedGlyph, _family.Setup.MinScale, _family.Setup.PxRange, _family.Setup.MiterLimit, _family.Setup.PaddingX, _family.Setup.PaddingY, &renderBox);

            using (_lock.EnterScope())
            {
                if (_unrenderedGlyphs.Count == 0)
                    UIManager.Instance.FontRenderer.RequestFontStyleRender(this);
                _unrenderedGlyphs.Add(new UnrenderedGlyph(codepoint, shapedGlyph, renderBox));
            }

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
                new Vector2((float)(vec[2] - vec[0]), (float)(vec[3] - vec[1])),
                Int2.MinValue,
                Vector4.NegativeInfinity,
                (float)(shapedGlyph->Advance * _metrics.UnitsPerEm),
                codepoint > 65534);
        }

        /// <summary>Not thread-safe</summary>
        private Vector2? GetKerningData(KerningKey key)
        {
            if (_family.GetGlyphContext(_style).TryGetKerningData(_weight, key.Left, key.Right, out Int2 kerning))
            {
                return kerning.AsVector2() * _metrics.UnitsPerEm;
            }

            return null;
        }

        /// <summary>Not thread-safe</summary>
        internal void ClearUnrenderedGlyphsSet()
        {
            _unrenderedGlyphs.Clear();
        }

        /// <summary>Not thread-safe</summary>
        internal void UpdateGlyphUVs(ReadOnlySpan<FontGlyphAtlasData> glyphs, bool updateAll = false)
        {
            Vector4 atlasSizeForMinMax = Vector4.One / new Vector4(_glyphAtlas.AtlasSize.X, _glyphAtlas.AtlasSize.Y, _glyphAtlas.AtlasSize.X, _glyphAtlas.AtlasSize.Y);

            for (int i = 0; i < glyphs.Length; ++i)
            {
                FontGlyphAtlasData atlasData = glyphs[i];
                _glyphsInAtlas.Add(atlasData);

                FontGlyph glyph;
                if (IsAsciiCodepoint(atlasData.Codepoint))
                {
                    glyph = _asciiGlyphs[atlasData.Codepoint - AsciiMinValue];
                    _asciiGlyphs[atlasData.Codepoint - AsciiMinValue] =
                        glyph = new FontGlyph(glyph.PlaneBounds, glyph.Dimensions, atlasData.AtlasRect.Size, atlasData.AtlasRect.AsBoundaries().AsVector4() * atlasSizeForMinMax, glyph.Advance, glyph.IsInvalid);
                }
                else
                {
                    glyph = _glyphs[atlasData.Codepoint];
                    _glyphs[atlasData.Codepoint] =
                        glyph = new FontGlyph(glyph.PlaneBounds, glyph.Dimensions, atlasData.AtlasRect.Size, atlasData.AtlasRect.AsBoundaries().AsVector4() * atlasSizeForMinMax, glyph.Advance, glyph.IsInvalid);
                }

                //FIXME: comparing to '\uffff' always returns false for some odd reason
                if (atlasData.Codepoint > 65534)
                {
                    _invalidGlyph = glyph;
                }
            }

            if (updateAll)
            {
                int startGlyphsLength = _glyphsInAtlas.Count - glyphs.Length;
                for (int i = 0; i < startGlyphsLength; ++i)
                {
                    FontGlyphAtlasData atlasData = _glyphsInAtlas[i];

                    FontGlyph glyph;
                    if (IsAsciiCodepoint(atlasData.Codepoint))
                    {
                        glyph = _asciiGlyphs[atlasData.Codepoint - AsciiMinValue];
                        _asciiGlyphs[atlasData.Codepoint - AsciiMinValue] =
                            glyph = new FontGlyph(glyph.PlaneBounds, glyph.Dimensions, atlasData.AtlasRect.Size, atlasData.AtlasRect.AsBoundaries().AsVector4() * atlasSizeForMinMax, glyph.Advance, glyph.IsInvalid); ;

                    }
                    else
                    {
                        glyph = _glyphs[atlasData.Codepoint];
                        _glyphs[atlasData.Codepoint] =
                            glyph = new FontGlyph(glyph.PlaneBounds, glyph.Dimensions, atlasData.AtlasRect.Size, atlasData.AtlasRect.AsBoundaries().AsVector4() * atlasSizeForMinMax, glyph.Advance, glyph.IsInvalid); ;
                    }

                    //FIXME: comparing to '\uffff' always returns false for some odd reason
                    if (atlasData.Codepoint > 65534)
                    {
                        _invalidGlyph = glyph;
                    }
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        public FontGlyph FindGlyph(char codepoint)
        {
            if (IsAsciiCodepoint(codepoint))
            {
                Debug.Assert(codepoint - AsciiMinValue < _asciiGlyphs.Length);

                ref FontGlyph glyph = ref _asciiGlyphs.DangerousGetReferenceAt(codepoint - AsciiMinValue);
                if (float.IsInfinity(glyph.Advance))
                    glyph = RenderNewGlyph(codepoint);

                if (glyph.IsInvalid)
                    return _invalidGlyph;
                else
                    return glyph;
            }
            else
            {
                FontGlyph glyph = _glyphs.GetOrAdd(codepoint, RenderNewGlyph);
                if (glyph.IsInvalid)
                    return _invalidGlyph;
                else
                    return glyph;
            }
        }

        /// <summary>Not thread-safe</summary>
        public Vector2 GetKerning(char leftCodepoint, char rightCodepoint, out bool exists)
        {
            Vector2? kerning = _kerning.GetOrAdd(new KerningKey(leftCodepoint, rightCodepoint), GetKerningData);
            exists = kerning.HasValue;
            return kerning.Value;
        }

        public FontFamily Family => _family;
        public FontStyle Style => _style;
        public FontWeight Weight => _weight;

        public FontStyleAdvances Advances => _advances;
        public FontStyleMetrics Metrics => _metrics;

        public FontGlyphAtlas GlyphAtlas => _glyphAtlas;

        internal ROHashSet<UnrenderedGlyph> UnrenderedGlyphs => _unrenderedGlyphs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAsciiCodepoint(char codepoint) => codepoint > AsciiMinValue && codepoint < AsciiMaxValue;

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

    public readonly record struct FontGlyph(Vector4 PlaneBounds, Vector2 Dimensions, Int2 PixelSize, Vector4 UVBounds, float Advance, bool IsInvalid);
    public readonly record struct KerningKey(char Left, char Right) : IEquatable<KerningKey>
    {
        public override int GetHashCode() => ((int)Left) << 16 | (int)Right;
    }

    public readonly record struct FontGlyphAtlasData(char Codepoint, Rect AtlasRect);
}
