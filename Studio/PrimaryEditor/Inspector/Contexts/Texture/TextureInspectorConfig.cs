using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Editor.Processors.Texture;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Editor;
using Tomlyn.Serialization;

namespace PrimaryEditor.Inspector.Contexts.Texture
{
    internal sealed class TextureInspectorConfig
    {
        [InspectorHidden, TomlIgnore]
        public AssetId TargetAssetId;

        public HandlingInfo Handling = new HandlingInfo();
        public MipmapsInfo Mipmaps = new MipmapsInfo();
        public VisualInfo Visual = new VisualInfo();

        public sealed class HandlingInfo
        {
            public TextureImageType ImageType = TextureImageType.Color;

            public TextureImageFormat ImageFormat = TextureImageFormat.Automatic;
            public TextureAlphaSource AlphaSource = TextureAlphaSource.Opaque;
            public TextureImageTypeSource TypeSource = TextureImageTypeSource.Default;

            public bool AlphaCutout = false;
            public byte CutoutThreshold = 127;
            public bool CutoutDither = false;

            public bool GammaCorrect = false;
            public bool PremultipliedAlpha = false;

            public bool FlipVertical = false;
        }

        public sealed class MipmapsInfo
        {
            public bool GenerateMipmaps = true;

            public TextureMipmapFilter MipmapFilter = TextureMipmapFilter.Box;

            public int MaxMipMapCount = int.MaxValue;
            public int MinMipMapSize = 1;

            public bool ScaleAlphaForMipMaps = false;
        }

        public sealed class VisualInfo
        {
            public TextureSwizzle Swizzle = TextureSwizzle.Default;

            public TextureReductionType ReductionType = TextureReductionType.Standard;
            public TextureFilterType MinFilter = TextureFilterType.Linear;
            public TextureFilterType MagFilter = TextureFilterType.Linear;
            public TextureFilterType MipFilter = TextureFilterType.Linear;

            public TextureAddressMode AddressModeU = TextureAddressMode.Repeat;
            public TextureAddressMode AddressModeV = TextureAddressMode.Repeat;
            public TextureAddressMode AddressModeW = TextureAddressMode.Repeat;

            public TextureComparisonFunction ComparisonFunction = TextureComparisonFunction.Never;

            public Color BorderColor = Color.TransparentBlack;

            public float MipLODBias = 1.0f;
            public float MinLOD = 0.0f;
            public float MaxLOD = float.MaxValue;

            public uint MaxAnisotropy = 0;
        }
    }

    [TomlSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        WriteIndented = true)]
    [TomlSerializable(typeof(TextureInspectorConfig))]
    internal partial class TextureInspectorConfigContext : TomlSerializerContext
    {
    }
}
