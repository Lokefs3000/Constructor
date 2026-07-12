using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Editor.Interop.MSDF;
using EditorUI.Text;
using EditorUI.Text.Visual;

namespace EditorUI.Assets.Loaders
{
    public static class FontFamilyLoader
    {
        public static unsafe FontFamily CreateFamilyFrom(FontFamilySetup setup, ReadOnlySpan<FontFamilyStyle> styles, IFontTextureFactory textureFactory)
        {
            FontStyleData?[] styleDatas = new FontStyleData[FontFamily.StyleDataLength];
            FontGlyphContext?[] glyphContexts = new FontGlyphContext[FontFamily.StyleDataLength / FontFamily.StylesPerGlyphContext];

            FontFamily fontFamily = new FontFamily(setup);

            foreach (FontFamilyStyle style in styles)
            {
                byte* rawData = (byte*)NativeMemory.Alloc((nuint)style.RawFontBlob.Length);
                style.RawFontBlob.CopyTo(new Span<byte>(rawData, style.RawFontBlob.Length));

                MSDF_FTContext* ft = MSDFInterop.InitFt();
                MSDF_FontFace* fontFace = MSDFInterop.LoadFont_Memory(ft, rawData, (ulong)style.RawFontBlob.Length);
                if (fontFace == null)
                {
                    MSDFInterop.DestroyFont(fontFace);
                    MSDFInterop.ShutdownFt(ft);

                    throw new FontFamilyLoadException($"Failed to load face '{style.Style}'");
                }

                MSDF_VarFontMetrics varFontMetrics = default;
                MSDF_VarFontData* fontData = MSDFInterop.GetVarFontData(fontFace, &varFontMetrics);

                FontGlyphStyle[] varStyles = new FontGlyphStyle[FontFamily.WeightCount];
                for (int i = 0; i < style.Weights.Length; i++)
                {
                    FontFamilyWeight weight = style.Weights[i];

                    MSDF_VarFontStyle varFontStyle = default;
                    MSDF_VarFontStyleData* varFontStyleData = MSDFInterop.GetVarFontStyle(fontFace, fontData, weight.StyleIndex, &varFontStyle);
                    varStyles[(int)weight.Weight] = new FontGlyphStyle(varFontStyleData, varFontStyle, weight.StyleIndex);

                    MSDFInterop.SetFontStyle(fontFace, weight.StyleIndex, varFontStyleData);

                    MSDFInterop.GetWhitespaceWidth(fontFace, out int spaceAdvance, out int tabAdvance);
                    MSDFInterop.GetMetrics(fontFace, out short ascender, out short descender, out short lineHeight, out short underlineY, out short height);
                    ushort unitsPerEm = MSDFInterop.GetUnitsPerEM(fontFace);

                    FontStyleAdvances advances = new FontStyleAdvances((short)spaceAdvance, (short)tabAdvance);
                    FontStyleMetrics metrics = new FontStyleMetrics((float)(1.0 / unitsPerEm), ascender, descender, lineHeight, underlineY, height);

                    styleDatas[(int)weight.Weight + (int)style.Style * FontFamily.WeightCount] = new FontStyleData(fontFamily, style.Style, weight.Weight, advances, metrics, textureFactory);
                }

                glyphContexts[(int)style.Style] = new FontGlyphContext(ft, fontFace, fontData, rawData, [.. varStyles]);
            }

            fontFamily.SetInternalData([.. styleDatas], [.. glyphContexts]);
            return fontFamily;
        }
    }

    public sealed class FontFamilyLoadException : Exception
    {
        public FontFamilyLoadException()
        {
        }

        public FontFamilyLoadException(string? message) : base(message)
        {
        }
    }

    public struct FontFamilyStyle
    {
        public FontStyle Style;
        public FontFamilyWeight[] Weights;

        public byte[] RawFontBlob;
    }

    public struct FontFamilyWeight
    {
        public FontWeight Weight;
        public uint StyleIndex;
    }
}
