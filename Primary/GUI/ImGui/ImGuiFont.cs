using Primary.Assets;
using Primary.Mathematics;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiFont
    {
        private TextureAsset _atlas;

        private FrozenDictionary<char, ImGuiGlyph> _glyphs;
        private ImGuiGlyph _badGlyph;

        internal ImGuiFont()
        {
            _atlas = AssetManager.LoadAsset<TextureAsset>("Engine/Textures/ImGui/FontAtlas.png").WaitIfNotLoaded();
            (_glyphs, _badGlyph) = CreateGlyphDict(new Vector2(_atlas.Width, _atlas.Height));
        }

        public ImGuiGlyph GetGlyph(char c) => _glyphs.TryGetValue(c, out ImGuiGlyph glyph) ? glyph : _badGlyph;

        public TextureAsset AtlasTexture => _atlas;

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        private static (FrozenDictionary<char, ImGuiGlyph>, ImGuiGlyph) CreateGlyphDict(Vector2 atlasSize)
        {
            string? source = AssetFilesystem.ReadString("Engine/Textures/ImGui/GlyphSet.json");
            if (source == null)
                return (FrozenDictionary<char, ImGuiGlyph>.Empty, default);

            List<ImGuiJsonGlyph>? tempGlyphs = JsonSerializer.Deserialize<List<ImGuiJsonGlyph>>(source, new JsonSerializerOptions
            {
                TypeInfoResolver = ImGuiFontGlyphJsonContext.Default,
            });

            if (tempGlyphs == null)
                throw new Exception();

            FrozenDictionary<char, ImGuiGlyph> glyphs = tempGlyphs.Select((x) =>
            {
                Vector4 planeBounds = (Vector4)x.PlaneBounds * FontHeight;
                Vector4 atlasBounds = x.AtlasUVs / new Vector4(atlasSize.X, atlasSize.Y, atlasSize.X, atlasSize.Y);

                planeBounds.Y += FontHeight * 0.5f;
                planeBounds.W += FontHeight * 0.5f;

                return new KeyValuePair<char, ImGuiGlyph>((char)x.Unicode, new ImGuiGlyph(planeBounds, atlasBounds, x.Advance * FontHeight));
            }).ToFrozenDictionary();

            return (glyphs, glyphs['\uffff']);

            /*UtilAddGlyph(' ', new Vector2(0, 0), new Vector2(4, 1), Vector2.Zero);
            UtilAddGlyph('!', new Vector2(9, 0), new Vector2(1, 7), Vector2.Zero);
            UtilAddGlyph('"', new Vector2(13, 0), new Vector2(3, 2), new Vector2(0, -5));
            UtilAddGlyph('#', new Vector2(18, 0), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('$', new Vector2(24, 0), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('%', new Vector2(30, 0), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('&', new Vector2(36, 0), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('\'', new Vector2(42, 0), new Vector2(1, 2), Vector2.Zero);
            UtilAddGlyph('(', new Vector2(48, 0), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph(')', new Vector2(54, 0), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('*', new Vector2(0, 9), new Vector2(3, 3), new Vector2(0, -2));
            UtilAddGlyph('+', new Vector2(6, 9), new Vector2(3, 3), new Vector2(0, -2));
            UtilAddGlyph(',', new Vector2(12, 13), new Vector2(2, 2), new Vector2(0, 1));
            UtilAddGlyph('-', new Vector2(18, 14), new Vector2(3, 1), new Vector2(0, -2));
            UtilAddGlyph('.', new Vector2(24, 14), new Vector2(1, 1), Vector2.Zero);
            UtilAddGlyph('/', new Vector2(30, 8), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('0', new Vector2(36, 8), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('1', new Vector2(42, 8), new Vector2(2, 7), Vector2.Zero);
            UtilAddGlyph('2', new Vector2(48, 8), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('3', new Vector2(54, 8), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('4', new Vector2(0, 16), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('5', new Vector2(6, 16), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('6', new Vector2(12, 16), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('7', new Vector2(18, 16), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('8', new Vector2(24, 16), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('9', new Vector2(30, 16), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph(':', new Vector2(36, 17), new Vector2(1, 5), Vector2.Zero);
            UtilAddGlyph(';', new Vector2(42, 17), new Vector2(2, 6), new Vector2(0, 1));
            UtilAddGlyph('<', new Vector2(48, 17), new Vector2(3, 5), new Vector2(0, -1));
            UtilAddGlyph('=', new Vector2(54, 18), new Vector2(3, 3), new Vector2(0, -2));
            UtilAddGlyph('>', new Vector2(0, 25), new Vector2(3, 5), new Vector2(0, -1));
            UtilAddGlyph('?', new Vector2(7, 24), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('@', new Vector2(12, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('A', new Vector2(18, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('B', new Vector2(24, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('C', new Vector2(30, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('D', new Vector2(36, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('E', new Vector2(42, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('F', new Vector2(48, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('G', new Vector2(54, 24), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('H', new Vector2(0, 32), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('I', new Vector2(6, 32), new Vector2(1, 7), Vector2.Zero);
            UtilAddGlyph('J', new Vector2(12, 32), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('K', new Vector2(18, 32), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('L', new Vector2(24, 32), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('M', new Vector2(30, 32), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('N', new Vector2(36, 32), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('O', new Vector2(42, 32), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('P', new Vector2(48, 32), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('Q', new Vector2(54, 32), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('R', new Vector2(0, 40), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('S', new Vector2(6, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('T', new Vector2(12, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('U', new Vector2(18, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('V', new Vector2(24, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('W', new Vector2(30, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('X', new Vector2(36, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('Y', new Vector2(42, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('Z', new Vector2(48, 40), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('[', new Vector2(54, 40), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('\\', new Vector2(0, 48), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph(']', new Vector2(6, 48), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('^', new Vector2(12, 48), new Vector2(5, 3), new Vector2(0, -4));
            UtilAddGlyph('_', new Vector2(18, 54), new Vector2(4, 1), Vector2.Zero);
            UtilAddGlyph('`', new Vector2(24, 48), new Vector2(2, 2), new Vector2(0, -5));
            UtilAddGlyph('a', new Vector2(30, 50), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('b', new Vector2(36, 48), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('c', new Vector2(42, 50), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('d', new Vector2(48, 48), new Vector2(5, 7), Vector2.Zero);
            UtilAddGlyph('e', new Vector2(54, 50), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('f', new Vector2(0, 56), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('g', new Vector2(17, 72), new Vector2(4, 7), new Vector2(0, 2));
            UtilAddGlyph('h', new Vector2(6, 56), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('i', new Vector2(12, 56), new Vector2(1, 7), Vector2.Zero);
            UtilAddGlyph('j', new Vector2(18, 56), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('k', new Vector2(24, 56), new Vector2(4, 7), Vector2.Zero);
            UtilAddGlyph('l', new Vector2(30, 56), new Vector2(2, 7), Vector2.Zero);
            UtilAddGlyph('m', new Vector2(36, 58), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('n', new Vector2(42, 58), new Vector2(3, 5), Vector2.Zero);
            UtilAddGlyph('o', new Vector2(47, 58), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('p', new Vector2(12, 72), new Vector2(4, 7), new Vector2(0, 2));
            UtilAddGlyph('q', new Vector2(55, 56), new Vector2(4, 7), new Vector2(0, 2));
            UtilAddGlyph('r', new Vector2(0, 66), new Vector2(4, 5), Vector2.Zero);
            UtilAddGlyph('s', new Vector2(6, 66), new Vector2(4, 5), Vector2.Zero);
            UtilAddGlyph('t', new Vector2(12, 65), new Vector2(3, 6), Vector2.Zero);
            UtilAddGlyph('u', new Vector2(18, 66), new Vector2(4, 5), Vector2.Zero);
            UtilAddGlyph('v', new Vector2(24, 66), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('w', new Vector2(30, 66), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('x', new Vector2(36, 66), new Vector2(5, 5), Vector2.Zero);
            UtilAddGlyph('y', new Vector2(42, 66), new Vector2(4, 5), Vector2.Zero);
            UtilAddGlyph('z', new Vector2(0, 2), new Vector2(4, 5), Vector2.Zero);
            UtilAddGlyph('{', new Vector2(48, 64), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('}', new Vector2(54, 64), new Vector2(3, 7), Vector2.Zero);
            UtilAddGlyph('~', new Vector2(0, 74), new Vector2(4, 2), new Vector2(0, -3));
            UtilAddGlyph('∞', new Vector2(22, 74), new Vector2(5, 3), new Vector2(0, -3));
            UtilAddGlyph('\uffff', new Vector2(6, 72), new Vector2(5, 7), Vector2.Zero);
            
            return (glyphs.ToFrozenDictionary(), glyphs['\uffff']);
            
            void UtilAddGlyph(char c, Vector2 atlasPosition, Vector2 spriteSize, Vector2 baseOffset)
            {
                baseOffset -= spriteSize - new Vector2(spriteSize.X, 7.0f);
            
                Vector2 sprMin = baseOffset;
                Vector2 sprMax = baseOffset + spriteSize;
            
                Vector2 uvMin = atlasPosition / atlasSize;
                Vector2 uvMax = uvMin + spriteSize / atlasSize;
            
                glyphs.Add(c, new ImGuiGlyph(new Vector4(sprMin.X, sprMin.Y, sprMax.X, sprMax.Y), new Vector4(uvMin.X, uvMin.Y, uvMax.X, uvMax.Y), spriteSize.X + 1.0f));
            }*/
        }

        public const float FontHeight = 16.0f;
        public const float FontVisualHeight = 11.0f;
    }

    public readonly record struct ImGuiGlyph(Vector4 PlaneBounds, Vector4 AtlasUVs, float Advance);

    internal record struct ImGuiJsonGlyph
    {
        [JsonPropertyName("unicode")] public ushort Unicode { get; set; }
        [JsonPropertyName("planeBounds")] public ImGuiJsonGlyphBounds PlaneBounds { get; set; }
        [JsonPropertyName("atlasBounds")] public ImGuiJsonGlyphBounds AtlasUVs { get; set; }
        [JsonPropertyName("advance")] public float Advance { get; set; }
    }

    internal record struct ImGuiJsonGlyphBounds
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public static implicit operator Vector4(ImGuiJsonGlyphBounds bounds) => Unsafe.ReadUnaligned<Vector4>(ref Unsafe.As<ImGuiJsonGlyphBounds, byte>(ref bounds));
    }

    [JsonSourceGenerationOptions()]
    [JsonSerializable(typeof(List<ImGuiJsonGlyph>))]
    internal partial class ImGuiFontGlyphJsonContext : JsonSerializerContext
    {

    }
}
