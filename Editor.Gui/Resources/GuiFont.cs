using Primary.Assets;
using Primary.Common.Streams;
using System.Collections.Frozen;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Editor.Gui.Resources
{
    public sealed class GuiFont
    {
        private TextureAsset? _textureAsset;
        private FrozenDictionary<char, GuiFontGlyph> _glyphs;

        private int _glyphSize;

        public GuiFont(BundleReader bundle, bool closeBundleAfterUse = false)
        {
            _textureAsset = null;
            _glyphs = FrozenDictionary<char, GuiFontGlyph>.Empty;

            _glyphSize = 0;

            if (!bundle.ContainsFile("FontGraphic") || !bundle.ContainsFile("FontData"))
                return;

            try
            {
                _textureAsset = AssetManager.LoadAsset<TextureAsset>("FontGraphic");
                _glyphs = LoadGlyphsAsDictionary(bundle, out _glyphSize).ToFrozenDictionary();
            }
            finally
            {
                if (closeBundleAfterUse)
                    bundle.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GuiFont(string bundlePath) : this(new BundleReader(AssetFilesystem.OpenStream(bundlePath)!, true, bundlePath), true) { }

        private static Dictionary<char, GuiFontGlyph> LoadGlyphsAsDictionary(BundleReader bundle, out int glyphSize)
        {
            string jsonText = bundle.ReadString("FontData")!;
            JsonNode rootNode = JsonNode.Parse(jsonText)!;

            JsonObject atlasObject = rootNode["atlas"]!.AsObject();

            Vector2 atlasSize = new Vector2(atlasObject["width"]!.GetValue<int>(), atlasObject["height"]!.GetValue<int>());
            glyphSize = atlasObject["size"]!.GetValue<int>();

            Dictionary<char, GuiFontGlyph> dict = new Dictionary<char, GuiFontGlyph>();
            foreach (JsonObject glyphObject in rootNode["glyphs"]!.AsArray()!)
            {
                char unicode = (char)glyphObject["unicode"]!.GetValue<int>();
                float advance = glyphObject["advance"]!.GetValue<float>();

                JsonObject? planeBounds = glyphObject["planeBounds"]?.AsObject();
                JsonObject? atlasBounds = glyphObject["atlasBounds"]?.AsObject();

                Vector4 boundaries = Vector4.Zero;
                Vector4 uvs = Vector4.Zero;

                if (planeBounds != null)
                {
                    Vector2 offset = new Vector2(planeBounds["left"]!.GetValue<float>(), planeBounds["top"]!.GetValue<float>());

                    boundaries = new Vector4(
                        offset.X, offset.Y + 1.0f,
                        planeBounds["right"]!.GetValue<float>(), planeBounds["bottom"]!.GetValue<float>() + 1.0f);
                }

                if (atlasBounds != null)
                {
                    uvs = new Vector4(
                        atlasBounds["left"]!.GetValue<float>(), atlasBounds["top"]!.GetValue<float>(),
                        atlasBounds["right"]!.GetValue<float>(), atlasBounds["bottom"]!.GetValue<float>()) / new Vector4(atlasSize.X, atlasSize.Y, atlasSize.X, atlasSize.Y);
                }

                dict.Add(unicode, new GuiFontGlyph(planeBounds != null && atlasBounds != null, advance, boundaries, uvs));
            }

            return dict;
        }

        public float CalculateTextWidth(ReadOnlySpan<char> text, float scale)
        {
            if (text.IsEmpty)
                return 0.0f;
            if (text.Length == 1)
            {
                ref readonly GuiFontGlyph glyph = ref TryGetGlyphData(text[0]);
                if (Unsafe.IsNullRef(in glyph))
                    return 0.0f;
                return glyph.Visible ? (glyph.Boundaries.Z - glyph.Boundaries.X) * scale : glyph.Advance * scale;
            }

            float width = 0.0f;

            int lastI = text.Length - 1;
            for (int i = 0; i < text.Length; i++)
            {
                ref readonly GuiFontGlyph glyph = ref TryGetGlyphData(text[i]);
                if (Unsafe.IsNullRef(in glyph))
                    continue;

                if (i == lastI)
                    width += glyph.Visible ? (glyph.Boundaries.Z - glyph.Boundaries.X) * scale : glyph.Advance * scale;
                else
                    width += glyph.Advance * scale;
            }

            return width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly GuiFontGlyph TryGetGlyphData(char c)
        {
            return ref _glyphs.GetValueRefOrNullRef(c);
        }

        public TextureAsset? Texture => _textureAsset;

        public int GlyphSize => _glyphSize;
    }

    public readonly record struct GuiFontGlyph(bool Visible, float Advance, Vector4 Boundaries, Vector4 UVs);
}
