using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Editor.Interop.MSDF;
using Primary.Assets;
using Primary.Common;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Loaders;

namespace PrimaryEditor.Processors.UIFontFamily
{
    public sealed class UIFontFamilyProcessor
    {
        public static unsafe void Execute(UIFontFamilyConfiguration config, Stream outputStream)
        {
            if (!Array.Exists(config.Fonts, static (x) => x.Style == UIFFStyle.Normal))
                throw new Exception("The required 'Normal' font style was not found");

            outputStream.Write(new UIFontFamilyHeader
            {
                FileHeader = UIFontFamilyHeader.Header,
                FileVersion = UIFontFamilyHeader.Version,

                DefaultStyle = default,
                RenderInfo = new UIFFRenderInfo
                {
                    GlyphSize = (ushort)config.GlyphSize,
                    DistanceRange = config.DistanceRange,
                    MiterLimit = config.MiterLimit
                },

                StyleCount = (byte)config.Fonts.Length
            });

            MSDF_FTContext* ft = MSDFInterop.InitFt();

            try
            {
                int allStyles = 0;
                foreach (UIFFCFont font in config.Fonts)
                {
                    int styleBit = 1 << ((int)font.Style >> 6);
                    if (Flags.HasFlag(allStyles, styleBit))
                        throw new Exception($"A font with the style '{font.Style}' has already been defined");
                    styleBit |= styleBit;

                    if (!Array.Exists(font.Weights, static (x) => x.Weight == 400))
                        throw new Exception($"The required style weight of '400' was not found");

                    if (!config.IdProvider!.TryGetLocalPathForId(font.Source, out string? localPath))
                        throw new Exception($"Failed to get local path for font style '{font.Source}'");

                    byte[]? rawData = FilesystemManager.ReadAllBytes(localPath);
                    if (rawData == null)
                        throw new Exception($"Failed to read bytes for font style '{localPath}'");

                    fixed (byte* rawDataPtr = rawData)
                    {
                        MSDF_FontFace* face = MSDFInterop.LoadFont_Memory(ft, rawDataPtr, (ulong)rawData.Length);
                        if (face == null)
                            throw new Exception($"Failed to load font style from '{localPath}'");

                        MSDF_VarFontMetrics varFontMetrics = default;
                        MSDF_VarFontData* varFontData = MSDFInterop.GetVarFontData(face, &varFontMetrics);

                        Dictionary<string, uint> styleDict = new Dictionary<string, uint>();
                        for (int i = 0; i < varFontMetrics.NamedStyleCount; i++)
                        {
                            MSDF_VarFontStyle varFontStyle = default;
                            MSDF_VarFontStyleData* varFontStyleData = MSDFInterop.GetVarFontStyle(face, varFontData, (uint)i, &varFontStyle);

                            styleDict.Add(varFontStyle.GetNameAsString(), (uint)i);
                        }

                        MSDFInterop.DestroyVarData(ft, varFontData);

                        outputStream.Write(new UIFFFontStyle
                        {
                            Style = font.Style,
                            WeightCount = (byte)font.Weights.Length,
                            BlobSize = rawData.Length
                        });

                        int allWeights = 0;
                        foreach (UIFFCWeight weight in font.Weights)
                        {
                            if (weight.Weight % 100 != 0)
                                throw new Exception($"Font weight '{weight.Weight}' must be a multiple of 100");
                            if (weight.Weight < 100 || weight.Weight > 900)
                                throw new Exception($"Font weight '{weight.Weight}' must be a number from 100 - 900");

                            if (!styleDict.TryGetValue(weight.SourceName, out uint index))
                                throw new Exception($"Font weight source '{weight.SourceName}' was not found in style '{localPath}'");

                            int weightIndex = weight.Weight / 100 - 1;

                            if (Flags.HasFlag(allWeights, 1 << weightIndex))
                                throw new Exception($"Duplicate weight '{weight.Weight}' defined");

                            outputStream.Write(new UIFFFontWeight
                            {
                                Weight = (UIFFWeight)weightIndex,
                                StyleIndex = index
                            });
                        }

                        outputStream.Write(rawData);
                    }
                }
            }
            finally
            {
                MSDFInterop.ShutdownFt(ft);
            }
        }
    }
}
