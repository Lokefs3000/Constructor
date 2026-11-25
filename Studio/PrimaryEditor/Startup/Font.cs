using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using K4os.Compression.LZ4.Streams;
using Primary.Assets;
using Primary.Common.Streams;
using Primary.Mathematics;
using SDL;

namespace PrimaryEditor.Startup
{
    internal unsafe sealed class Font
    {
        private Image _image;
        private Dictionary<char, FontGlyph> _glyphs;

        private float _lineHeight;
        private float _underlineY;

        internal Font(SDL_Renderer* renderer, BundleReader reader, string fontName)
        {
            _image = new Image(renderer, reader, $"{fontName}.bmp.lz4", true);
            _glyphs = new Dictionary<char, FontGlyph>();

            SDL3.SDL_SetTextureBlendMode(_image.Pixels, SDL_BlendMode.SDL_BLENDMODE_BLEND);

            {
                using Stream source = new MemoryStream(reader.ReadBytes($"{fontName}.json")!);

                JsonNode node = JsonNode.Parse(source)!;

                JsonObject atlasObject = node["atlas"]!.AsObject();
                float size = atlasObject["size"]!.GetValue<int>();

                Vector2 atlasSize = new Vector2(
                    atlasObject["width"]!.GetValue<int>(),
                    atlasObject["height"]!.GetValue<int>());

                JsonObject metricsObject = node["metrics"]!.AsObject();
                _lineHeight = metricsObject["lineHeight"]!.GetValue<float>() * size;
                _underlineY = metricsObject["underlineY"]!.GetValue<float>() * size;

                foreach (JsonObject? glyphObject in node["glyphs"]!.AsArray()!)
                {
                    JsonNode? unicodeNode = glyphObject["index"] ?? glyphObject["unicode"];
                    if (unicodeNode != null)
                    {
                        char unicode = (char)unicodeNode!.GetValue<int>();
                        Boundaries planeBounds = Boundaries.Zero;
                        Rect atlasBounds = Rect.Zero;

                        JsonObject? planeBoundsObject = glyphObject["planeBounds"]?.AsObject();
                        if (planeBoundsObject != null)
                        {
                            planeBounds = new Boundaries
                            {
                                Minimum = new Vector2(
                                    planeBoundsObject["left"]!.GetValue<float>(),
                                    -planeBoundsObject["top"]!.GetValue<float>()
                                    ) * size,
                                Maximum = new Vector2(
                                    planeBoundsObject["right"]!.GetValue<float>(),
                                    -planeBoundsObject["bottom"]!.GetValue<float>()
                                    ) * size,
                            };
                        }

                        JsonObject? atlasBoundsObject = glyphObject["atlasBounds"]?.AsObject();
                        if (atlasBoundsObject != null)
                        {
                            atlasBounds = new Boundaries(
                                    new Vector2(atlasBoundsObject["left"]!.GetValue<float>(), atlasSize.Y - atlasBoundsObject["top"]!.GetValue<float>()),
                                    new Vector2(atlasBoundsObject["right"]!.GetValue<float>(), atlasSize.Y - atlasBoundsObject["bottom"]!.GetValue<float>())).AsRect();
                        }

                        _glyphs.Add(unicode, new FontGlyph(glyphObject["advance"]!.GetValue<float>() * size, planeBounds, atlasBounds, planeBoundsObject != null && atlasBoundsObject != null));
                    }
                }
            }
        }

        internal bool TryGetGlyph(char c, [NotNullWhen(true)] out FontGlyph value)
        {
            return _glyphs.TryGetValue(c, out value);
        }

        internal Image Image => _image;
    }

    internal readonly record struct FontGlyph(float Advance, Boundaries PlaneBounds, Rect AtlasBounds, bool IsRendered);
}
