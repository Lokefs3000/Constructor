using Primary.Assets;
using Primary.Common;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Editor.LegacyGui.Data
{
    public sealed class GuiFont
    {
        private readonly TextureAsset? _graphic;
        private readonly FrozenDictionary<char, GuiFontGlyph> _glyphs;

        internal GuiFont(string imagePath, string dataPath)
        {
            _graphic = AssetManager.LoadAsset<TextureAsset>(imagePath);
            _glyphs = GenerateGlyphDictionary(dataPath).ToFrozenDictionary();
        }

        private static Dictionary<char, GuiFontGlyph> GenerateGlyphDictionary(string dataPath)
        {
            string? jsonSource = AssetFilesystem.ReadString(dataPath);
            if (jsonSource == null)
                return new Dictionary<char, GuiFontGlyph>();

            JsonNode rootNode = NullableUtility.ThrowIfNull(JsonNode.Parse(jsonSource));
            JsonObject atlasNode = NullableUtility.ThrowIfNull(rootNode["atlas"]?.AsObject());

            Vector2 atlasSize = new Vector2(atlasNode["width"]!.GetValue<int>(), atlasNode["height"]!.GetValue<int>());
      
            Dictionary<char, GuiFontGlyph> dict = new Dictionary<char, GuiFontGlyph>();

            foreach (JsonObject? @object in NullableUtility.ThrowIfNull(rootNode["glyphs"]).AsArray())
            {
                Debug.Assert(@object != null);

                char unicode = (char)NullableUtility.ThrowIfNull(@object["unicode"]).GetValue<ushort>();
                float advance = NullableUtility.ThrowIfNull(@object["advance"]).GetValue<float>();

                GuiFontGlyph glyph = new GuiFontGlyph
                {
                    Advance = advance,
                };

                if (NullableUtility.GetIfNotNull(@object["planeBounds"]?.AsObject(), out JsonObject planeBounds))
                {
                    float left = planeBounds["left"]!.GetValue<float>();
                    float top = planeBounds["top"]!.GetValue<float>();
                    float right = planeBounds["right"]!.GetValue<float>();
                    float bottom = planeBounds["bottom"]!.GetValue<float>();

                    glyph.Offset = new Vector2(left, top);
                    glyph.Size = new Vector2(right, bottom) - new Vector2(left, top);
                }

                if (NullableUtility.GetIfNotNull(@object["atlasBounds"]?.AsObject(), out JsonObject atlasBounds))
                {
                    float left = atlasBounds["left"]!.GetValue<float>();
                    float top = atlasBounds["top"]!.GetValue<float>();
                    float right = atlasBounds["right"]!.GetValue<float>();
                    float bottom = atlasBounds["bottom"]!.GetValue<float>();

                    glyph.Visible = true;
                    glyph.UVs = new Vector4(left, top, right, bottom) / new Vector4(atlasSize.X, atlasSize.Y, atlasSize.X, atlasSize.Y);
                }

                dict[unicode] = glyph;
            }

            return dict;
        }

        public float CalculateTextWidth(ReadOnlySpan<char> text, float size)
        {
            if (text.IsEmpty)
                return 0.0f;

            float x = 0.0f;
            int last = text.Length - 1;

            //TODO: update 4 at a time using SIMD vectors?
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                ref readonly GuiFontGlyph glyph = ref _glyphs.GetValueRefOrNullRef(c);

                if (Unsafe.IsNullRef(in glyph))
                    continue;

                //this is here so that it doesnt get oversized
                //the glyph will not have a size if its invisible though
                if (i == last && glyph.Visible)
                    x += glyph.Size.X * size;
                else
                    x += glyph.Advance * size;
            }

            return x;
        }

        public ref readonly GuiFontGlyph TryGetGlyph(char c) => ref _glyphs.GetValueRefOrNullRef(c);

        public TextureAsset? Graphic => _graphic;
    }

    public record struct GuiFontGlyph
    {
        public bool Visible;

        public Vector2 Offset;
        public Vector2 Size;

        public float Advance;

        public Vector4 UVs;
    }
}
