using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Assets.Utility;
using Tomlyn.Serialization;

namespace PrimaryEditor.Processors.UIFontFamily
{
    public sealed class UIFontFamilyConfiguration
    {
        [TomlIgnore]
        public IAssetIdProvider? IdProvider { get; set; } = null;

        public int GlyphSize { get; set; } = 0;
        public float DistanceRange { get; set; } = 0.0f;
        public float MiterLimit { get; set; } = 0.0f;
        public UIFFCFont[] Fonts { get; set; } = [];
    }

    public sealed class UIFFCFont
    {
        [TomlConverter(typeof(EarlyAssetIdTomlConverter))]
        public AssetId Source { get; set; } = AssetId.Invalid;
        public UIFFStyle Style { get; set; } = UIFFStyle.Normal;
        public UIFFCWeight[] Weights { get; set; } = [];
    }

    public sealed class UIFFCWeight
    {
        public string SourceName { get; set; } = string.Empty;
        public int Weight { get; set; } = 400;
    }
}
