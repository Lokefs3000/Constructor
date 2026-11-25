using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Primary.Mathematics;
using Primary.Serialization.Toml;
using PrimaryEditor.Assets.Utility;
using Tomlyn.Serialization;

namespace PrimaryEditor.Processors.TextureAtlas
{
    public sealed class TextureAtlasConfiguration
    {
        [TomlRequired, TomlConverter(typeof(EarlyAssetIdTomlConverter))]
        public AssetId Texture { get; set; } = AssetId.Invalid;
        public TextureAtlasSprite[] Sprites { get; set; } = [];
    }

    public sealed class TextureAtlasSprite
    {
        public string Name { get; set; } = string.Empty;
        [TomlConverter(typeof(Int2TomlConverter))]
        public Int2 Offset { get; set; } = default;
        [TomlConverter(typeof(Int2TomlConverter))]
        public Int2 Size { get; set; } = default;
    }
}
