using CommunityToolkit.HighPerformance;
using Editor.Assets;
using Editor.Storage;
using Editor.UI.Assets.Loaders;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;
using Tomlyn.Syntax;

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

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
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

            using Stream? fontFile = filesystem.OpenStream(config.FontFile);
            if (fontFile == null)
                return false;

            using Stream? outputStream = FileUtility.TryWaitOpenNoThrow(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            if (outputStream == null)
                return false;

            {
                UIFontHeader header = new UIFontHeader
                {
                    Header = UIFontHeader.ConstHeader,
                    Version = UIFontHeader.ConstVersion,

                    GlyphSize = config.GlyphSize,

                    DistanceRange = config.DistanceRange,
                    MiterLimit = config.MiterLimit,

                    FontFileSize = (int)fontFile.Length,
                    DefaultStyleLength = (byte)config.DefaultStyle.Length
                };

                outputStream.Write(header);
                outputStream.Write(Encoding.UTF8.GetBytes(config.DefaultStyle));
                fontFile.CopyTo(outputStream);
            }

            filesystem.RemapFile(localInputFile, localOutputFile);

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);
            pipeline.Associator.MakeAssocation(id, pipeline.Identifier.GetOrRegisterAsset(config.FontFile), true);

            AssetDatabase database = Editor.GlobalSingleton.AssetDatabase;
            database.AddEntry<UIFontAsset>(new AssetDatabaseEntry(id, localInputFile, true));

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetDatabase database = Editor.GlobalSingleton.AssetDatabase;
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);

            database.AddEntry<UIFontAsset>(new AssetDatabaseEntry(id, localFilePath, ValidateFile(localFilePath, filesystem, pipeline)));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);
            if (stream == null)
                return false;

            UIFontHeader header = stream.Read<UIFontHeader>();
            return header.Header == UIFontHeader.ConstHeader && header.Version == UIFontHeader.ConstVersion && header.GlyphSize >= 1 && stream.Position + header.FontFileSize <= stream.Length;
        }

        public string? CustomFileIcon => null;
    }

    internal sealed class UIFontAssetConfig
    {
        [TomlRequired]
        public string FontFile { get; set; }

        [TomlRequired]
        public int GlyphSize { get; set; }

        [TomlRequired]
        public string DefaultStyle { get; set; }

        public float DistanceRange { get; set; }
        public float MiterLimit { get; set; }

        public UIFontAssetConfig()
        {
            FontFile = string.Empty;

            GlyphSize = -1;

            DefaultStyle = string.Empty;

            DistanceRange = 2.0f;
            MiterLimit = 1.0f;
        }
    }

    [TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    [TomlSerializable(typeof(UIFontAssetConfig))]
    internal partial class UIFontAssetConfigContext : TomlSerializerContext
    {

    }
}
