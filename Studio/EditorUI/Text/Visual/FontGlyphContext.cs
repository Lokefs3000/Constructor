using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Editor.Interop.MSDF;

namespace EditorUI.Text.Visual
{
    internal unsafe sealed class FontGlyphContext : IDisposable
    {
        private readonly MSDF_FTContext* _ft;
        private readonly MSDF_FontFace* _face;
        private readonly MSDF_VarFontData* _varData;
        private readonly ImmutableArray<FontGlyphStyle> _weights;

        private Lock _lock;

        private FontWeight _currentWeight;

        private bool _disposedValue;

        internal FontGlyphContext(MSDF_FTContext* ft, MSDF_FontFace* face, MSDF_VarFontData* varData, ImmutableArray<FontGlyphStyle> weights)
        {
            _ft = ft;
            _face = face;
            _varData = varData;
            _weights = weights;

            _lock = new Lock();

            _currentWeight = (FontWeight)0xff;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                MSDFInterop.DestroyVarFontData(_ft, _varData);
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
                    MSDFInterop.SetFontStyle(_face, glyph, _weights[(int)weight].StyleData);
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
    }

    internal unsafe readonly struct FontGlyphStyle
    {
        public readonly MSDF_VarFontStyleData* StyleData;
        public readonly MSDF_VarFontStyle Style;

        public FontGlyphStyle(MSDF_VarFontStyleData* styleData, MSDF_VarFontStyle style)
        {
            StyleData = styleData;
            Style = style;
        }
    }
}
