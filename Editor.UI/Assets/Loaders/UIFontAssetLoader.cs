using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using System.Collections.Frozen;
using System.Diagnostics;
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
                Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom);
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

                string defaultStyleName;
                {
                    byte[] tmp = new byte[header.DefaultStyleLength];
                    stream.ReadExactly(tmp);

                    defaultStyleName = Encoding.UTF8.GetString(tmp);
                }

                byte[] loadedMemory = new byte[header.FontFileSize];
                stream.ReadExactly(loadedMemory);

                nint ft = EdInterop.MSDF_InitFt();

                MSDF_FontFace* face;
                fixed (byte* tempPtr = loadedMemory)
                    face = EdInterop.MSDF_LoadFont_Memory(ft, tempPtr, (ulong)header.FontFileSize);

                if (face == null)
                {
                    EdInterop.MSDF_ShutdownFt(ft);
                    fontData.UpdateAssetFailed(font);
                    return;
                }

                MSDF_VarFontMetrics metrics = default;
                nint vars = EdInterop.MSDF_GetVarFontData(face, &metrics);

                if (vars == nint.Zero)
                {
                    EdInterop.MSDF_DestroyFont(face);
                    EdInterop.MSDF_ShutdownFt(ft);

                    fontData.UpdateAssetFailed(font);
                    return;
                }

                Dictionary<string, UIFontStyle> styles = new Dictionary<string, UIFontStyle>((int)(metrics.NamedStyleCount + 1));
                for (int i = 0; i < metrics.NamedStyleCount; i++)
                {
                    MSDF_VarFontStyle styleData = default;
                    nint stylePtr = EdInterop.MSDF_GetVarFontStyle(face, vars, (uint)i, &styleData);

                    Debug.Assert(stylePtr != nint.Zero);

                    double spaceAdvance = 0.0;
                    double tabAdvance = 0.0;
                    double ascender = 0.0;
                    double descender = 0.0;
                    double lineHeight = 0.0;
                    double underlineY = 0.0;
                    double height = 0.0;

                    EdInterop.MSDF_SetFontStyle(face, (uint)i, stylePtr);
                    EdInterop.MSDF_GetWhitespaceWidth(face, &spaceAdvance, &tabAdvance);
                    EdInterop.MSDF_GetMetrics(face, &ascender, &descender, &lineHeight, &underlineY, &height);

                    string name = string.Empty;
                    if (styleData.Name != null)
                    {
                        char* strBe = (char*)styleData.Name;
                        using RentedArray<char> strLe = RentedArray<char>.Rent((int)(styleData.NameLength / 2));

                        for (int j = 0; j < strLe.Count; j++)
                            strLe[j] = (char)(((strBe[j] & 0xff) << 8) | ((strBe[j] & 0xff00) >> 8));

                        name = strLe.Span.ToString();
                    }

                    FontStyleAdvances styleAdvances = new FontStyleAdvances((float)spaceAdvance, (float)tabAdvance);
                    FontStyleMetrics styleMetrics = new FontStyleMetrics((float)ascender, (float)descender, (float)lineHeight, (float)underlineY, (float)height);

                    styles.Add(name, new UIFontStyle(font, fontData, name, i, styleAdvances, styleMetrics));
                }

                if (!styles.ContainsKey(string.Empty))
                    styles.Add("", styles[defaultStyleName]);

                EdInterop.MSDF_SetFontPixelSize(face, 0, (uint)header.GlyphSize);

                MSDF_ShapedGlyph* shapedGlyph = EdInterop.MSDF_CreateShapedGlyph();

                fontData.UpdateAssetData(font, ft, (nint)face, vars, header.GlyphSize, shapedGlyph, styles.ToFrozenDictionary());
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                fontData.UpdateAssetFailed(font);
                EngLog.Assets.Error(ex, "Failed to load ui font: {name}", sourcePath);
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

        public int FontFileSize;
        public byte DefaultStyleLength;

        public const uint ConstHeader = 0x54464955;
        public const uint ConstVersion = 1;
    }
}
