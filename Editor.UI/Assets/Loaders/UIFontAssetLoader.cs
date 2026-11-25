using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Editor.UI.Assets.Loaders
{
    internal sealed class UIFontAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new UIFontAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if(assetData is not UIFontAssetData fontData)
                throw new ArgumentException(nameof(assetData));

            return new UIFontAsset(fontData);
        }

        public unsafe void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not UIFontAsset font)
                throw new ArgumentException(nameof(asset));
            if (assetData is not UIFontAssetData fontData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom);
                if (stream == null)
                {
                    fontData.UpdateAssetFailed(font);
                    return;
                }

                UIFontHeader header = stream.Read<UIFontHeader>();
                if (header.Header != UIFontHeader.ConstHeader || header.Version != UIFontHeader.ConstVersion)
                {
                    fontData.UpdateAssetFailed(font);
                    return;
                }

                byte[] headerRawData = new byte[header.DataStart];
                byte[] sourceRawData = new byte[stream.Length - header.DataStart];

                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.ReadExactly(headerRawData);
                    stream.ReadExactly(sourceRawData);
                }

                MemoryStream headerStream = new MemoryStream(headerRawData, false);
                MemoryStream sourceStream = new MemoryStream(sourceRawData, false);

                headerStream.Seek(Unsafe.SizeOf<UIFontHeader>(), SeekOrigin.Begin);

                int foundStyles = 0;

                List<UIFontTypeData> types = new List<UIFontTypeData>();
                for (int i = 0; i < header.TypeCount; i++)
                {
                    UIFontType typeHeader = headerStream.Read<UIFontType>();

                    double emScale = 1.0 / typeHeader.UnitsPerEM;

                    Dictionary<char, int> codepoints = new Dictionary<char, int>();
                    for (int j = 0; j < typeHeader.LetterRangeCount; j++)
                    {
                        UIFontLetterRange letterRange = headerStream.Read<UIFontLetterRange>();

                        char end = (char)(letterRange.StartCodepoint + letterRange.CodepointCount);
                        for (char k = (char)letterRange.StartCodepoint; k < end; k++)
                        {
                            codepoints.Add(k, (int)sourceStream.Position);

                            UIFontGlyph glyph = sourceStream.Read<UIFontGlyph>();
                            sourceStream.Seek(glyph.DataSize, SeekOrigin.Current);
                        }
                    }

                    FontStyleAdvances advances = new FontStyleAdvances(
                        (float)(typeHeader.Advances.SpaceAdvance * emScale),
                        (float)(typeHeader.Advances.TabAdvance * emScale));

                    FontStyleMetrics metrics = new FontStyleMetrics(
                        emScale,
                        (float)(typeHeader.Metrics.Ascender * emScale),
                        (float)(typeHeader.Metrics.Descender * emScale),
                        (float)(typeHeader.Metrics.LineHeight * emScale),
                        (float)(typeHeader.Metrics.UnderlineY * emScale),
                        (float)(typeHeader.Metrics.Height * emScale));

                    UIFontTypeData typeData = new UIFontTypeData(font, fontData, (FontStyle)typeHeader.Type.Style, (FontWeight)typeHeader.Type.Weight, codepoints.ToFrozenDictionary(), advances, metrics);
                    types.Add(typeData);

                    foundStyles |= 1 << (int)typeData.Style;
                }

                types.Sort(static (x, y) =>
                {
                    int r;
                    if ((r = x.Style.CompareTo(y.Style)) != 0)
                        return r;
                    else
                        return x.Weight.CompareTo(y.Weight);
                });

                UIFontStyleData[] styleData = new UIFontStyleData[int.PopCount(foundStyles)];

                int offset = 0;
                for (int i = 0; i < styleData.Length; i++)
                {
                    UIFontTypeData[] weights = new UIFontTypeData[9];
                    for (int j = offset; j < types.Count; j++)
                    {
                        UIFontTypeData typeData = types[j];
                        if (typeData.Style != (FontStyle)i)
                        {
                            offset = j;
                            break;
                        }
                        else
                        {
                            weights[(int)typeData.Weight] = typeData;
                        }
                    }

                    for (int j = 0; j < weights.Length; j++)
                    {
                        if (weights[j] == null)
                            weights[j] = weights[(int)FontWeight.Normal];
                    }

                    Guard.IsNotNull(weights[(int)FontWeight.Normal]);

                    styleData[i] = new UIFontStyleData([.. weights]);
                }

                fontData.UpdateAssetData(font, sourceRawData, header.GlyphSize, styleData);
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                fontData.UpdateAssetFailed(font);
                UIManager.Logger?.Error(ex, "Failed to load ui font: {name}", sourcePath);
            }
#endif
        }
    }

    public struct UIFontHeader
    {
        public uint Header;
        public uint Version;

        public int GlyphSize;

        public float DistanceRange;
        public float MiterLimit;

        public UIFontTarget DefaultStyle;
        public byte TypeCount;

        public int DataStart;

        public const uint ConstHeader = 0x54464955;
        public const uint ConstVersion = 2;
    }

    public struct UIFontType
    {
        public UIFontTarget Type;
        public ushort LetterRangeCount;
        public ushort UnitsPerEM;

        public UIFontTypeAdvances Advances;
        public UIFontTypeMetrics Metrics;
    }

    public struct UIFontTypeAdvances
    {
        public int SpaceAdvance;
        public int TabAdvance;
    }

    public struct UIFontTypeMetrics
    {
        public short Ascender;
        public short Descender;
        public short LineHeight;
        public short UnderlineY;
        public short Height;
    }

    public struct UIFontLetterRange
    {
        public ushort StartCodepoint;
        public ushort CodepointCount;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct UIFontGlyph
    {
        [FieldOffset(0)]
        public byte ContourCount;
        [FieldOffset(4)]
        public int Advance;
        [FieldOffset(8)]
        public int DataSize;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct UIFontContour
    {
        [FieldOffset(0)]
        public byte EdgeCount;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct UIFontPoint
    {
        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;
    }

    public struct UIFontTarget(UIFontStyle style, UIFontWeight weight)
    {
        public byte Code = (byte)(((int)style << 6) | (int)weight);

        public readonly UIFontStyle Style => (UIFontStyle)((Code >> 6) & 0x3);
        public readonly UIFontWeight Weight => (UIFontWeight)(Code & 0x3f);
    }

    public enum UIFontEdgeType : byte
    {
        /// <summary>2 points (ushort)</summary>
        Linear = 0,
        /// <summary>3 points (ushort)</summary>
        Quadratic,
        /// <summary>4 points (ushort)</summary>
        Cubic
    }

    public enum UIFontStyle : byte
    {
        Normal = 0,
        Italic
    }
    
    public enum UIFontWeight : byte
    {
        _100 = 0,
        _200,
        _300,
        _400,
        _500,
        _600,
        _700,
        _800,
        _900,

        Lighter = _300,
        Normal = _400,
        Bold = _700,
        Bolder = _800
    }
}
