using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Editor;
using Primary.Serialization.Toml;
using PrimaryEditor.Assets.Utility;
using Tomlyn;
using Tomlyn.Serialization;

namespace Editor.Processors.Texture
{
    //NOTE: as of Tomlyn 2.2.1 the source generator does not respect custom converters or atleast the generated output doesnt seem to.

    //[TomlSourceGenerationOptions(
    //    Converters = [typeof(AssetIdTomlConverter), typeof(TextureSwizzleConverter), typeof(ColorTomlConverter)],
    //    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    //[TomlSerializable(typeof(TextureConfiguration))]
    //[TomlSerializable(typeof(CompositeConfiguration))]
    //[TomlSerializable(typeof(CubemapConfiguration))]
    //public partial class TextureTomlContext : TomlSerializerContext
    //{

    public sealed class TextureSwizzleConverter : TomlConverter<TextureSwizzle>
    {
        public override TextureSwizzle Read(TomlReader reader)
        {
            if (reader.TokenType != TomlTokenType.StartArray)
                throw reader.CreateException("Expected array start for texture swizzle");

            ushort code = 0;

            int shift = 9;
            while (true)
            {
                if (!reader.Read())
                    throw reader.CreateException("Expected channel for texture swizzle");

                string str = reader.GetString();
                if (Enum.TryParse(str, out TextureSwizzleChannel channel))
                    code |= (ushort)(((int)channel) << shift);
                else
                    throw reader.CreateException($"Failed to parse channel {(9 - shift) / 3} with {str} of texture swizzle");

                if (shift == 0)
                    break;
                shift -= 3;
            }

            if (!reader.Read() || reader.TokenType != TomlTokenType.EndArray)
                throw reader.CreateException("Expected array end for texture swizzle");

            reader.Read();
            return new TextureSwizzle(code);
        }

        public override void Write(TomlWriter writer, TextureSwizzle value)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.R.ToString());
            writer.WriteStringValue(value.G.ToString());
            writer.WriteStringValue(value.B.ToString());
            writer.WriteStringValue(value.A.ToString());
            writer.WriteEndArray();
        }
    }
    //}

    [TomlPolymorphic(UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.Fail)]
    [TomlDerivedType(typeof(CompositeConfiguration), "composite")]
    [TomlDerivedType(typeof(CubemapConfiguration), "cubemap")]
    public class TextureConfiguration
    {
        [TomlIgnore]
        public AssetId DefaultId { get; set; } = AssetId.Invalid;
        [TomlIgnore]
        public IAssetIdProvider? IdProvider { get; set; } = null;

        [TomlRequired, TomlPropertyName("handling")]
        public Handling HandlingInfo { get; set; } = new Handling();
        [TomlPropertyName("mipmaps")]
        public Mipmaps MipmapsInfo { get; set; } = new Mipmaps();
        [TomlPropertyName("visual")]
        public Visual VisualInfo { get; set; } = new Visual();

        public struct Handling()
        {
            [TomlRequired]
            public TextureImageType ImageType { get; set; }

            public TextureImageFormat ImageFormat { get; set; } = TextureImageFormat.Automatic;
            public TextureAlphaSource AlphaSource { get; set; } = TextureAlphaSource.Opaque;
            public TextureImageTypeSource Source { get; set; } = TextureImageTypeSource.Default;

            public bool AlphaCutout { get; set; } = false;
            public byte CutoutThreshold { get; set; } = 127;
            public bool CutoutDither { get; set; } = false;

            public bool GammaCorrect { get; set; } = false;
            public bool PremultipliedAlpha { get; set; } = false;

            public bool FlipVertical { get; set; } = false;
        }

        public struct Mipmaps()
        {
            public bool GenerateMipmaps { get; set; } = true;

            public TextureMipmapFilter MipmapFilter { get; set; } = TextureMipmapFilter.Box;

            public int MaxMipMapCount { get; set; } = int.MaxValue;
            public int MinMipMapSize { get; set; } = 1;

            public bool ScaleAlphaForMipMaps { get; set; } = false;
        }

        public struct Visual()
        {
            public TextureSwizzle Swizzle { get; set; } = TextureSwizzle.Default;

            public TextureReductionType ReductionType { get; set; } = TextureReductionType.Standard;
            public TextureFilterType MinFilter { get; set; } = TextureFilterType.Linear;
            public TextureFilterType MagFilter { get; set; } = TextureFilterType.Linear;
            public TextureFilterType MipFilter { get; set; } = TextureFilterType.Linear;

            public TextureAddressMode AddressModeU { get; set; } = TextureAddressMode.Repeat;
            public TextureAddressMode AddressModeV { get; set; } = TextureAddressMode.Repeat;
            public TextureAddressMode AddressModeW { get; set; } = TextureAddressMode.Repeat;

            public TextureComparisonFunction ComparisonFunction { get; set; } = TextureComparisonFunction.Never;

            public Color BorderColor { get; set; } = Color.TransparentBlack;

            [TomlPropertyName("mip_lod_bias")]
            public float MipLODBias { get; set; } = 1.0f;
            [TomlPropertyName("min_lod")]
            public float MinLOD { get; set; } = 0.0f;
            [TomlPropertyName("max_lod")]
            public float MaxLOD { get; set; } = float.MaxValue;

            public uint MaxAnisotropy { get; set; } = 0;
        }
    }

    public sealed class CompositeConfiguration : TextureConfiguration
    {
        [TomlRequired, TomlPropertyName("composite")]
        public Composite CompositeInfo { get; set; } = new Composite();

        public struct Composite()
        {
            [TomlRequired]
            public TextureCompositeChannel Channels { get; set; }

            public CompsiteChannel Red { get; set; } = new CompsiteChannel(TextureCompositeChannel.Red);
            public CompsiteChannel Green { get; set; } = new CompsiteChannel(TextureCompositeChannel.Green);
            public CompsiteChannel Blue { get; set; } = new CompsiteChannel(TextureCompositeChannel.Blue);
            public CompsiteChannel Alpha { get; set; } = new CompsiteChannel(TextureCompositeChannel.Alpha);
        }

        public struct CompsiteChannel(TextureCompositeChannel sourceChannel)
        {
            [TomlConverter(typeof(EarlyFileIdTomlConverter))]
            public FileId Asset { get; set; } = FileId.Invalid;
            public TextureCompositeChannel Source { get; set; } = sourceChannel;
            public bool Invert { get; set; } = false;
        }
    }

    public sealed class CubemapConfiguration : TextureConfiguration
    {
        [TomlRequired, TomlPropertyName("cubemap")]
        public Cubemap CubemapInfo { get; set; } = new Cubemap();
        [TomlPropertyName("composited")]
        public Composited CompositedInfo { get; set; } = new Composited();

        public struct Cubemap()
        {
            [TomlRequired]
            public TextureCubemapSource Source { get; set; }
        }

        public struct Composited()
        {
            [TomlRequired, TomlConverter(typeof(EarlyFileIdTomlConverter))] public FileId PositiveX { get; set; }
            [TomlRequired, TomlConverter(typeof(EarlyFileIdTomlConverter))] public FileId PositiveY { get; set; }
            [TomlRequired, TomlConverter(typeof(EarlyFileIdTomlConverter))] public FileId PositiveZ { get; set; }

            [TomlRequired, TomlConverter(typeof(EarlyFileIdTomlConverter))] public FileId NegativeX { get; set; }
            [TomlRequired, TomlConverter(typeof(EarlyFileIdTomlConverter))] public FileId NegativeY { get; set; }
            [TomlRequired, TomlConverter(typeof(EarlyFileIdTomlConverter))] public FileId NegativeZ { get; set; }
        }
    }

    public enum TextureImageFormat : byte
    {
        Automatic = 0,

        BC7,
        BC6s,
        BC6u,
        BC5u,
        BC4u,
        BC3,
        BC3n,
        BC2,
        BC1a,
        BC1,
        R8a,
        RG8,
        RGB8,
        RGBA8,
        R16,
        RG16,
        RGBA16,
        R32,
        RG32,
        RGBA32
    }

    public enum TextureAlphaSource : byte
    {
        None = 0,
        Opaque,
        Source,
        Red
    }

    public enum TextureMipmapFilter : byte
    {
        Box = 0,
        Kaiser,
        Triangle,
        Mitchell,
        Min,
        Max
    }

    public enum TextureImageType : byte
    {
        Color = 0,
        Grayscale,
        Normal,
        Specular,
        [InspectorHidden]
        Cubemap
    }

    public enum TextureImageTypeSource : byte
    {
        Default = 0,

        NormalTangent,
        NormalObject,
        NormalBump,
    }

    [Flags]
    public enum TextureCompositeChannel : byte
    {
        None = 0,

        Red = 1 << 0,
        Green = 1 << 1,
        Blue = 1 << 2,
        Alpha = 1 << 3,
    }

    public enum TextureCubemapSource : byte
    {
        Composited = 0
    }
}
