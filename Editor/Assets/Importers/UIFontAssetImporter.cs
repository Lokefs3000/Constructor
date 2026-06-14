using CommunityToolkit.HighPerformance;
using Editor.Assets;
using Editor.Interop.Ed;
using Editor.Storage;
using Editor.UI.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

namespace Editor.UI.Assets.Importers
{
    internal sealed class UIFontAssetImporter : IAssetImporter
    {
        public UIFontAssetImporter()
        {

        }

        public void Dispose()
        {

        }

        public unsafe bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);
            filesystem.RemapFile(localInputFile, null);

            using Stream? stream = filesystem.OpenStream(localInputFile);
            if (stream == null)
                return false;

            if (!TomlSerializer.TryDeserialize(stream, UIFontAssetConfigContext.Default, out UIFontAssetConfig? config) || config == null)
                return false;

            if (config.GlyphSize < 1)
                return false;

            nint ft = EdInterop.MSDF_InitFt();
            if (ft == nint.Zero)
                return false;

            Ptr<MSDF_FontFace>[] faces = new Ptr<MSDF_FontFace>[config.Fonts.Length];
            MSDF_ShapedGlyph* shapedGlyph = EdInterop.MSDF_CreateShapedGlyph();
            nint vars = nint.Zero;

            List<(Ptr<MSDF_Vector2>, uint)> points = new List<(Ptr<MSDF_Vector2>, uint)>();

            MemoryStream headerStream = new MemoryStream();
            MemoryStream dataStream = new MemoryStream();

            {
                headerStream.Write(new UIFontHeader
                {
                    Header = UIFontHeader.ConstHeader,
                    Version = UIFontHeader.ConstVersion,

                    GlyphSize = config.GlyphSize,

                    DistanceRange = config.DistanceRange,
                    MiterLimit = config.MiterLimit,

                    DefaultStyle = default,
                    TypeCount = (byte)config.Fonts.Sum(static (x) => x.Weights.Length),

                    DataStart = -1
                });
            }

            try
            {
                int i = 0;
                foreach (FontFileConfig fontFile in config.Fonts)
                {
                    using Stream? sourceStream = filesystem.OpenStream(fontFile.Source);
                    if (sourceStream == null)
                        return false;

                    byte[] rawData = new byte[sourceStream.Length];
                    sourceStream.ReadExactly(rawData);

                    Ptr<MSDF_FontFace> ptr;
                    fixed (byte* memory = rawData)
                    {
                        ptr = EdInterop.MSDF_LoadFont_Memory(ft, memory, (ulong)sourceStream.Length);
                        if (ptr.IsNull)
                            return false;

                        faces[i++] = ptr;
                    }

                    MSDF_VarFontMetrics metrics = default;
                    vars = EdInterop.MSDF_GetVarFontData(ptr, &metrics);
                    if (vars == nint.Zero)
                        return false;

                    Dictionary<string, int> styleIndices = new Dictionary<string, int>();
                    for (int j = 0; j < metrics.NamedStyleCount; j++)
                    {
                        MSDF_VarFontStyle styleData = default;
                        nint stylePtr = EdInterop.MSDF_GetVarFontStyle(ptr, vars, (uint)j, &styleData);

                        string name = string.Empty;
                        if (styleData.Name != null)
                        {
                            char* strBe = (char*)styleData.Name;
                            using RentedArray<char> strLe = RentedArray<char>.Rent((int)(styleData.NameLength / 2));

                            for (int k = 0; k < strLe.Count; k++)
                                strLe[k] = (char)(((strBe[k] & 0xff) << 8) | ((strBe[k] & 0xff00) >> 8));

                            name = strLe.Span.ToString();
                        }

                        styleIndices.Add(name, j);
                    }

                    for (int j = 0; j < fontFile.Weights.Length; j++)
                    {
                        FontFileStyle style = fontFile.Weights[j];
                        int sourceIndex = styleIndices[style.SourceName];

                        MSDF_VarFontStyle styleData = default;
                        nint stylePtr = EdInterop.MSDF_GetVarFontStyle(ptr, vars, (uint)sourceIndex, &styleData);

                        EdInterop.MSDF_SetFontStyle(ptr, (uint)sourceIndex, stylePtr);

                        int spaceAdvance = 0;
                        int tabAdvance = 0;
                        short ascender = 0;
                        short descender = 0;
                        short lineHeight = 0;
                        short underlineY = 0;
                        short height = 0;

                        EdInterop.MSDF_GetWhitespaceWidth(ptr, &spaceAdvance, &tabAdvance);
                        EdInterop.MSDF_GetMetrics(ptr, &ascender, &descender, &lineHeight, &underlineY, &height);

                        UIFontTarget target = new UIFontTarget(fontFile.Style, (UIFontWeight)((style.Weight / 100) - 1));

                        long fontTypePosition = headerStream.Position;
                        headerStream.Seek(Unsafe.SizeOf<UIFontType>(), SeekOrigin.Current);

                        int rangeCount = 0;

                        char startCodepoint = '\0';
                        long lastCodepointOffset = -1;

                        // TODO: load font charmap instead of this!!
                        const char MaxIterationValue = (char)256;
                        for (char c = '\0'; c <= MaxIterationValue; c++)
                        {
                            bool r = EdInterop.MSDF_ShapeGlyph(ptr, c, shapedGlyph);
                            if (r)
                            {
                                if (lastCodepointOffset == -1)
                                {
                                    startCodepoint = c;
                                    lastCodepointOffset = headerStream.Position;
                                }

                                int contourCount = (int)EdInterop.MSDF_QueryShapeContours(shapedGlyph);
                                long backupPosition = dataStream.Position;

                                dataStream.Write(new UIFontGlyph
                                {
                                    ContourCount = 0,
                                    Advance = 0,
                                    DataSize = 0
                                });

                                int dataSizeStart = (int)dataStream.Position;
                                int writtenLength = 0;

                                MSDF_EdgeData edgeData = default;
                                for (int contour = 0; contour < contourCount; contour++)
                                {
                                    int contourEdgeCount = (int)EdInterop.MSDF_QueryContourEdges(shapedGlyph, (uint)contour);

                                    dataStream.Write(new UIFontContour { EdgeCount = (byte)contourEdgeCount });
                                    writtenLength += Unsafe.SizeOf<UIFontContour>();

                                    for (int edge = 0; edge < contourEdgeCount; edge++)
                                    {
                                        EdInterop.MSDF_GetContourEdges(shapedGlyph, (uint)contour, (uint)edge, &edgeData);

                                        switch (edgeData.Type)
                                        {
                                            case 1: dataStream.Write(UIFontEdgeType.Linear); break;
                                            case 2: dataStream.Write(UIFontEdgeType.Quadratic); break;
                                            case 3: dataStream.Write(UIFontEdgeType.Cubic); break;
                                        }

                                        int len = (int)(edgeData.Type + 1);
                                        for (int k = 0; k < len; ++k)
                                        {
                                            UIFontPoint point = new UIFontPoint { X = (int)edgeData.Points[k].X, Y = (int)edgeData.Points[k].Y };
                                            dataStream.Write(point);
                                        }

                                        writtenLength += Unsafe.SizeOf<UIFontEdgeType>() + Unsafe.SizeOf<UIFontPoint>() * len;
                                    }
                                }

                                Debug.Assert((dataStream.Position - dataSizeStart) == writtenLength);

                                dataStream.Seek(backupPosition, SeekOrigin.Begin);
                                dataStream.Write(new UIFontGlyph
                                {
                                    ContourCount = (byte)contourCount,
                                    Advance = (int)shapedGlyph->Advance,
                                    DataSize = (int)(dataStream.Length - dataSizeStart)
                                });
                                dataStream.Seek(0, SeekOrigin.End);
                            }
                            
                            if ((!r || c == MaxIterationValue) && lastCodepointOffset != -1)
                            {
                                headerStream.Write(new UIFontLetterRange
                                {
                                    StartCodepoint = startCodepoint,
                                    CodepointCount = c == MaxIterationValue ? ((ushort)(c - startCodepoint + 1)) : (ushort)(c - startCodepoint)
                                });

                                lastCodepointOffset = -1;
                                ++rangeCount;
                            }
                        }

                        headerStream.Seek(fontTypePosition, SeekOrigin.Begin);
                        headerStream.Write(new UIFontType
                        {
                            Type = target,
                            LetterRangeCount = (ushort)rangeCount,
                            UnitsPerEM = EdInterop.MSDF_GetUnitsPerEM(ptr),
                            Advances = new UIFontTypeAdvances
                            {
                                SpaceAdvance = spaceAdvance,
                                TabAdvance = tabAdvance,
                            },
                            Metrics = new UIFontTypeMetrics
                            {
                                Ascender = ascender,
                                Descender = descender,
                                LineHeight = lineHeight,
                                UnderlineY = underlineY,
                                Height = height,
                            }
                        });

                        headerStream.Seek(0, SeekOrigin.End);
                    }

                    EdInterop.MSDF_DestroyVarData(ft, vars);
                    vars = nint.Zero;
                }

                headerStream.Seek(0, SeekOrigin.Begin);
                headerStream.Write(new UIFontHeader
                {
                    Header = UIFontHeader.ConstHeader,
                    Version = UIFontHeader.ConstVersion,

                    GlyphSize = config.GlyphSize,

                    DistanceRange = config.DistanceRange,
                    MiterLimit = config.MiterLimit,

                    DefaultStyle = default,
                    TypeCount = (byte)config.Fonts.Sum(static (x) => x.Weights.Length),

                    DataStart = (int)headerStream.Length
                });
            }
            finally
            {
                if (vars != nint.Zero)
                    EdInterop.MSDF_DestroyVarData(ft, vars);

                if (shapedGlyph != null)
                    EdInterop.MSDF_DestroyShapedGlyph(shapedGlyph);

                foreach (Ptr<MSDF_FontFace> ptr in faces)
                {
                    if (!ptr.IsNull)
                        EdInterop.MSDF_DestroyFont(ptr.Pointer);
                }

                EdInterop.MSDF_ShutdownFt(ft);
            }

            {
                using Stream? outputStream = FileUtility.TryWaitOpenNoThrow(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                if (outputStream == null)
                    return false;

                using RentedArray<byte> bytes = RentedArray<byte>.Rent(4096);
                int read;

                headerStream.Seek(0, SeekOrigin.Begin);
                dataStream.Seek(0, SeekOrigin.Begin);

                while ((read = headerStream.Read(bytes.Span)) > 0)
                    outputStream.Write(bytes.Span[..read]);
                while ((read = dataStream.Read(bytes.Span)) > 0)
                    outputStream.Write(bytes.Span[..read]);
            }

            filesystem.RemapFile(localInputFile, localOutputFile);

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);
            {
                AssetId[] ids = config.Fonts.Select((x) => pipeline.Identifier.GetOrRegisterAsset(x.Source)).ToArray();
                pipeline.Associator.MakeAssocations(id, ids, true);
            }

            AssetDatabase database = EditorRuntime.GlobalSingleton.AssetDatabase;
            database.AddEntry<UIFontAsset>(new AssetDatabaseEntry(id, localInputFile, true));

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetDatabase database = EditorRuntime.GlobalSingleton.AssetDatabase;
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);

            database.AddEntry<UIFontAsset>(new AssetDatabaseEntry(id, localFilePath, ValidateFile(localFilePath, filesystem, pipeline)));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);
            if (stream == null || stream.Length < Unsafe.SizeOf<UIFontHeader>())
                return false;

            UIFontHeader header = stream.Read<UIFontHeader>();
            return header.Header == UIFontHeader.ConstHeader && header.Version == UIFontHeader.ConstVersion && header.GlyphSize >= 1 && stream.Position + header.GlyphSize <= stream.Length;
        }

        public string? CustomFileIcon => null;
    }

    internal sealed class UIFontAssetConfig
    {
        [TomlRequired]
        public string DefaultStyle { get; set; } = string.Empty;

        [TomlRequired]
        public int GlyphSize { get; set; } = 32;

        [TomlRequired]
        public float DistanceRange { get; set; } = 2.0f;

        [TomlRequired]
        public float MiterLimit { get; set; } = 1.0f;

        [TomlRequired]
        public FontFileConfig[] Fonts { get; set; } = [];
    }

    internal sealed class FontFileConfig
    {
        [TomlRequired]
        public string Source { get; set; } = string.Empty;

        [TomlRequired]
        public UIFontStyle Style { get; set; } = UIFontStyle.Normal;

        [TomlRequired]
        public FontFileStyle[] Weights { get; set; } = [];
    }

    internal sealed class FontFileStyle
    {
        [TomlRequired]
        public string SourceName { get; set; } = string.Empty;

        [TomlRequired]
        public int Weight { get; set; } = 400;
    }

    [TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    [TomlSerializable(typeof(UIFontAssetConfig))]
    internal partial class UIFontAssetConfigContext : TomlSerializerContext
    {

    }
}
