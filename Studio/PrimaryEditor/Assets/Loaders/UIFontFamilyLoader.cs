using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Assets;
using EditorUI.Assets.Loaders;
using EditorUI.Text;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Utility;
using PrimaryEditor.Rendering.UI;

namespace PrimaryEditor.Assets.Loaders
{
    public sealed class UIFontFamilyLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new UIFontFamilyData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            return new UIFontFamilyAsset((UIFontFamilyData)assetData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            UIFontFamilyAsset fontFamily = (UIFontFamilyAsset)asset;
            UIFontFamilyData fontFamilyData = (UIFontFamilyData)assetData;

            using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom)
                ?? throw new AssetLoadException("Failed to open stream for reading");

            if (!stream.TryRead(out UIFontFamilyHeader headerData))
                throw new AssetLoadException("Issue occured reading header");

            if (headerData.FileHeader != UIFontFamilyHeader.Header)
                throw new AssetLoadException($"Invalid header present in file '{headerData.FileHeader:x8}'");
            if (headerData.FileVersion != UIFontFamilyHeader.Version)
                throw new AssetLoadException($"Invalid version present in file '{headerData.FileVersion}'");

            using RentedArray<FontFamilyStyle> styles = RentedArray<FontFamilyStyle>.Rent(headerData.StyleCount);
            for (int i = 0; i < headerData.StyleCount; i++)
            {
                if (!stream.TryRead(out UIFFFontStyle fontStyle))
                    throw new AssetLoadException("Issue occured reading font style");

                using RentedArray<FontFamilyWeight> array = RentedArray<FontFamilyWeight>.Rent(fontStyle.WeightCount);
                for (int j = 0; j < fontStyle.WeightCount; j++)
                {
                    if (!stream.TryRead(out UIFFFontWeight fontWeight))
                        throw new AssetLoadException("Issue occured reading font weight");

                    array[j] = new FontFamilyWeight() {
                        Weight = (FontWeight)fontWeight.Weight,
                        StyleIndex = fontWeight.StyleIndex
                    };
                }

                byte[] blobBuffer = new byte[fontStyle.BlobSize];
                stream.ReadExactly(blobBuffer);

                styles[i] = new FontFamilyStyle
                {
                    Style = (FontStyle)((int)fontStyle.Style >> 6),
                    Weights = [.. array],

                    RawFontBlob = blobBuffer
                };
            }

            FontFamilySetup setup = new FontFamilySetup(headerData.RenderInfo.GlyphSize, headerData.RenderInfo.DistanceRange, headerData.RenderInfo.MiterLimit, 2, 2);
            FontFamily family = FontFamilyLoader.CreateFamilyFrom(setup, styles.Span, new FontTextureFactory());

            fontFamilyData.UpdateAssetData(fontFamily, family);
        }
    }

    public struct UIFontFamilyHeader
    {
        public int FileHeader;
        public int FileVersion;

        public UIFFStyleData DefaultStyle;
        public UIFFRenderInfo RenderInfo;

        public byte StyleCount;

        public const int Header = 0x46464955;
        public const int Version = 1;
    }

    public struct UIFFRenderInfo
    {
        public ushort GlyphSize;
        public float DistanceRange;
        public float MiterLimit;
    }

    public struct UIFFFontStyle
    {
        public UIFFStyle Style;
        public byte WeightCount;
        public int BlobSize;
    }

    public struct UIFFFontWeight
    {
        public UIFFWeight Weight;
        public uint StyleIndex;
    }

    public struct UIFFStyleData
    {
        public byte Code;

        public UIFFStyle Style { get => (UIFFStyle)(Code & 0xc0); set => Code = (byte)((Code & ~0xc0) | (int)value); }
        public UIFFWeight Weight { get => (UIFFWeight)(Code & 0x3f); set => Code = (byte)((Code & ~0x3f) | (int)value); }
    }

    public enum UIFFStyle : byte
    {
        Normal = 0 << 6,
        Italic = 1 << 6,
    }

    public enum UIFFWeight
    {
        w100 = 0,
        w200,
        w300,
        w400,
        w500,
        w600,
        w700,
        w800,
        w900,

        Light = w300,
        Normal = w400,
        Bold = w600,
        Bolder = w700
    }
}
