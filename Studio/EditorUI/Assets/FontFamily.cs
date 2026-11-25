using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Text;
using EditorUI.Text.Visual;

namespace EditorUI.Assets
{
    public sealed class FontFamily : IDisposable
    {
        private ImmutableArray<FontStyleData?> _styleDatas;
        private ImmutableArray<FontGlyphContext?> _glyphContexts;

        private readonly FontFamilySetup _setup;

        private bool _disposedValue;

        internal FontFamily(FontFamilySetup setup)
        {
            _setup = setup;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    FontRenderer fontRenderer = UIManager.Instance.FontRenderer;
                    foreach (FontStyleData? styleData in _styleDatas)
                    {
                        if (styleData != null)
                        {
                            fontRenderer.CancelFontStyleRender(styleData);
                            styleData.Dispose();
                        }
                    }

                    foreach (FontGlyphContext? glyphContext in _glyphContexts)
                    {
                        glyphContext?.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void SetInternalData(ImmutableArray<FontStyleData?> styleDatas, ImmutableArray<FontGlyphContext?> glyphContexts)
        {
            Debug.Assert(styleDatas.Length == StyleDataLength);
            Debug.Assert(glyphContexts.Length == styleDatas.Length / StylesPerGlyphContext);

            Guard.IsNotNull(styleDatas[(int)FontWeight.Normal]);
            Guard.IsNotNull(glyphContexts[(int)FontStyle.Normal]);

            _styleDatas = styleDatas;
            _glyphContexts = glyphContexts;
        }

        public FontStyleData GetFontStyle(FontStyle style, FontWeight weight) => _styleDatas[(int)style * 9 + (int)weight] ?? _styleDatas[(int)FontWeight.Normal]!;
        internal FontGlyphContext GetGlyphContext(FontStyle style) => _glyphContexts[(int)style] ?? _glyphContexts[0]!;

        public FontFamilySetup Setup => _setup;

        internal const int StyleDataLength = 9 * 2; // weights * styles
        internal const int StylesPerGlyphContext = 9; // weights * styles

        internal const int WeightCount = 9;
    }

    public readonly record struct FontFamilySetup(int MinScale, double PxRange, double MiterLimit, int PaddingX, int PaddingY);
}
