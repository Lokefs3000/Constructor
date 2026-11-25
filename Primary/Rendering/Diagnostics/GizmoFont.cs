using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Mathematics;

namespace Primary.Rendering.Diagnostics
{
    public sealed class GizmoFont
    {
        private readonly TextureAsset _fontTexture;
        private readonly FrozenDictionary<char, GizmoGlyph> _glyphDict;

        private readonly ImmutableArray<GizmoGlyph> _asciiArray;

        internal GizmoFont()
        {
            _fontTexture = AssetManager.LoadAsset<TextureAsset>("Engine/Textures/Debug/GizmoTextAtlas.png")
                .WaitIfNotLoaded();
            (_glyphDict, _asciiArray) = BuildGlyphs();
        }

        private (FrozenDictionary<char, GizmoGlyph>, ImmutableArray<GizmoGlyph>) BuildGlyphs()
        {
            if (!_fontTexture.IsLoaded)
                return (FrozenDictionary<char, GizmoGlyph>.Empty, []);

            Dictionary<char, GizmoGlyph> glyphs = new Dictionary<char, GizmoGlyph>();
            GizmoGlyph[] asciiGlyphs = new GizmoGlyph[AsciiEnd - AsciiStart];

            string? source = AssetFilesystem.ReadString("Engine/Textures/Debug/GizmoTextAtlas.dat");
            if (source != null)
            {
                Int2 cursor = Int2.Zero;
                Vector2 invSize = Vector2.One / new Vector2(_fontTexture.Width, _fontTexture.Height);

                foreach (var token in source.Tokenize('\n'))
                {
                    if (ushort.TryParse(token, out ushort result))
                    {
                        if (cursor.X + GlyphWidth > _fontTexture.Width)
                        {
                            cursor.X = 0;
                            cursor.Y += GlyphHeight + 1;
                        }

                        Int2 maxCursor = new Int2(cursor.X + GlyphWidth, cursor.Y + GlyphHeight);

                        GizmoGlyph glyph = new GizmoGlyph(
                            new Vector2(cursor.X, maxCursor.Y) * invSize,
                            new Vector2(maxCursor.X, cursor.Y) * invSize);

                        glyphs.TryAdd((char)result, glyph);
                        if (result >= AsciiStart && result < AsciiEnd)
                        {
                            asciiGlyphs[result - AsciiStart] = glyph;
                        }

                        cursor.X = maxCursor.X + 1;
                    }
                }
            }

            return (glyphs.ToFrozenDictionary(), asciiGlyphs.ToImmutableArray());
        }

        public bool TryFindGlyph(char unicode, out GizmoGlyph glyph)
        {
            int ascii = unicode - AsciiStart;
            if (ascii < _asciiArray.Length)
            {
                glyph = _asciiArray.AsSpan().DangerousGetReferenceAt(ascii);
                return true;
            }
            else
            {
                return _glyphDict.TryGetValue(unicode, out glyph);
            }
        }

        public TextureAsset Texture => _fontTexture;

        public const int GlyphWidth = 7;
        public const int GlyphHeight = 13;

        public const int GlyphAdvance = GlyphWidth + 1;
        public const int GlyphLineHeight = GlyphHeight + 1;

        private const char AsciiStart = (char)0x20;
        private const char AsciiEnd = (char)0x7f;
    }

    public readonly record struct GizmoGlyph(Vector2 UVMinimum, Vector2 UVMaximum);
}
