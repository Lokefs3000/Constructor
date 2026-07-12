using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Editor.Interop.MSDF;
using Primary.Mathematics;

namespace EditorUI.Text.Visual
{
    public unsafe sealed class FontGlyphContext : IDisposable
    {
        private readonly MSDF_FTContext* _ft;
        private readonly MSDF_FontFace* _face;
        private readonly MSDF_VarFontData* _varData;
        private readonly byte* _rawData;
        private readonly ImmutableArray<FontGlyphStyle> _weights;

        private Lock _lock;

        private FontWeight _currentWeight;

        private bool _disposedValue;

        internal FontGlyphContext(MSDF_FTContext* ft, MSDF_FontFace* face, MSDF_VarFontData* varData, byte* rawData, ImmutableArray<FontGlyphStyle> weights)
        {
            _ft = ft;
            _face = face;
            _varData = varData;
            _rawData = rawData;
            _weights = weights;

            _lock = new Lock();

            _currentWeight = (FontWeight)0xff;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_rawData != null)
                    NativeMemory.Free(_rawData);
                MSDFInterop.DestroyVarData(_ft, _varData);
                MSDFInterop.DestroyFont(_face);
                MSDFInterop.ShutdownFt(_ft);

                _disposedValue = true;
            }
        }

        ~FontGlyphContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Thread-safe</summary>
        internal MSDF_ShapedGlyph* ShapeNewGlyph(FontWeight weight, char glyph)
        {
            using (_lock.EnterScope())
            {
                if (_currentWeight != weight)
                {
                    MSDFInterop.SetFontStyle(_face, _weights[(int)weight].Index, _weights[(int)weight].StyleData);
                    _currentWeight = weight;
                }

                MSDF_ShapedGlyph* shapedGlyph = MSDFInterop.CreateShapedGlyph();
                if (!MSDFInterop.ShapeGlyph(_face, glyph, shapedGlyph))
                {
                    MSDFInterop.DestroyShapedGlyph(shapedGlyph);
                    return null;
                }

                return shapedGlyph;
            }
        }

        /// <summary>Thread-safe</summary>
        internal bool TryGetKerningData(FontWeight weight, char left, char right, out Int2 kerning)
        {
            using (_lock.EnterScope())
            {
                if (_currentWeight != weight)
                {
                    MSDFInterop.SetFontStyle(_face, _weights[(int)weight].Index, _weights[(int)weight].StyleData);
                    _currentWeight = weight;
                }

                MSDF_KernData kernData = new MSDF_KernData();
                if (!MSDFInterop.GetKerning(_face, left, right, out kernData) || (kernData.X == 0 && kernData.Y == 0))
                {
                    kerning = default;
                    return false;
                }

                kerning = new Int2(kernData.X, kernData.Y);
                return true;
            }
        }
    }

    internal unsafe readonly struct FontGlyphStyle
    {
        public readonly MSDF_VarFontStyleData* StyleData;
        public readonly MSDF_VarFontStyle Style;
        public readonly uint Index;

        public FontGlyphStyle(MSDF_VarFontStyleData* styleData, MSDF_VarFontStyle style, uint index)
        {
            StyleData = styleData;
            Style = style;
            Index = index;
        }
    }
}
