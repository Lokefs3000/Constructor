using CommunityToolkit.HighPerformance;
using Editor.Assets;
using Editor.Interop.Ed;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Silk.NET.Assimp;
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
                string? source = AssetFilesystem.ReadString(sourcePath, bundleToReadFrom);
                if (source == null)
                {
                    fontData.UpdateAssetFailed(font);
                    return;
                }

                TomlTable table = Toml.ToModel<TomlTable>(source, sourcePath);

                if (!table.TryGetValue("font_file", out object fontFile) || fontFile is not string)
                {
                    fontData.UpdateAssetFailed(font);
                    return;
                }

                if (!table.TryGetValue("glyph_size", out object glyphSize) || glyphSize is not long)
                {
                    fontData.UpdateAssetFailed(font);
                    return;
                }

                nint ft = EdInterop.MSDF_InitFt();

                if (!AssetPipeline.TryGetFullPathFromLocal((string)fontFile, out string? fullPath))
                {
                    fontData.UpdateAssetFailed(font);
                    return;
                }

                MSDF_FontFace* face = LoadFontStackalloc(ft, fullPath);
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
                    double lineHeight = 0.0;

                    EdInterop.MSDF_SetFontStyle(face, (uint)i, stylePtr);
                    EdInterop.MSDF_GetWhitespaceWidth(face, &spaceAdvance, &tabAdvance);
                    EdInterop.MSDF_GetLineHeight(face, &lineHeight);

                    string name = string.Empty;
                    if (styleData.Name != null)
                    {
                        char* strBe = (char*)styleData.Name;
                        using RentedArray<char> strLe = RentedArray<char>.Rent((int)(styleData.NameLength / 2));

                        for (int j = 0; j < strLe.Count; j++)
                            strLe[j] = (char)(((strBe[j] & 0xff) << 8) | ((strBe[j] & 0xff00) >> 8));

                        name = strLe.Span.ToString();
                    }

                    styles.Add(name, new UIFontStyle(font, fontData, name, i, (float)spaceAdvance, (float)tabAdvance, (float)lineHeight));
                }

                if (!styles.ContainsKey(string.Empty))
                    styles.Add("", styles["Regular"]);

                EdInterop.MSDF_SetFontPixelSize(face, 0, (uint)(long)glyphSize);

                MSDF_ShapedGlyph* shapedGlyph = EdInterop.MSDF_CreateShapedGlyph();

                fontData.UpdateAssetData(font, ft, (nint)face, vars, (int)(long)glyphSize, shapedGlyph, styles.ToFrozenDictionary());
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

            static MSDF_FontFace* LoadFontStackalloc(nint ft, string fullPath)
            {
                sbyte* data = stackalloc sbyte[fullPath.Length + 1];

                for (int i = 0; i < fullPath.Length; ++i)
                    data[i] = (sbyte)fullPath[i];
                data[fullPath.Length] = (sbyte)'\0';

                return EdInterop.MSDF_LoadFont(ft, data);
            }
        }
    }
}
