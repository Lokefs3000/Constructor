using CommunityToolkit.HighPerformance;
using Editor.Storage;
using Editor.UI.Assets;
using Editor.UI.Assets.Importers;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.Serialization.Toml;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

namespace Editor.Assets.Importers
{
    internal sealed class TextureAtlasAssetImporter : IAssetImporter
    {
        public TextureAtlasAssetImporter()
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

            TextureAtlasConfiguration? config = TomlSerializer.Deserialize<TextureAtlasConfiguration>(stream, s_serializerOptions);
            if (config == null)
                return false;

            AssetId textureAssetId;
            {
                if (Guid.TryParse(config.Texture, out Guid result))
                    textureAssetId = new AssetId(result);
                else
                    textureAssetId = pipeline.Identifier.RetriveIdForPath(config.Texture);

                if (!pipeline.Identifier.IsIdValid(textureAssetId))
                    return false;
            }

            string? texturePath = pipeline.Identifier.RetrievePathForId(textureAssetId);
            if (texturePath == null || !pipeline.TryGetImporter(texturePath, out IAssetImporter? importer) || importer is not TextureAssetImporter)
                return false;

            MemoryStream dataStream = new MemoryStream();

            dataStream.Write(new TexAtlasHeader
            {
                FileHeader = TexAtlasHeader.Header,
                FileVersion = TexAtlasHeader.Version,

                TextureId = textureAssetId,
                SpriteCount = (ushort)config.Sprites.Length
            });

            foreach (TextureAtlasSprite sprite in config.Sprites)
            {
                ReadOnlySpan<char> nameSpan = sprite.Name;
                if (nameSpan.Length > byte.MaxValue)
                {
                    EdLog.Assets.Warning("[{p}]: Atlas sprite name is longer than >{max} characters and will be trimmed {name}", localInputFile, byte.MaxValue, sprite.Name);
                    nameSpan = nameSpan[..byte.MaxValue];
                }

                dataStream.Write(new TexAtlasSprite
                {
                    NameLength = (byte)nameSpan.Length,

                    Offset = new TexAtlasVector
                    {
                        X = (ushort)sprite.Offset.X,
                        Y = (ushort)sprite.Offset.Y,
                    },
                    Size = new TexAtlasVector
                    {
                        X = (ushort)sprite.Size.X,
                        Y = (ushort)sprite.Size.Y,
                    },
                });

                if (!nameSpan.IsEmpty)
                    dataStream.Write(MemoryMarshal.Cast<char, byte>(nameSpan));
            }

            {
                using Stream? outputStream = FileUtility.TryWaitOpenNoThrow(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                if (outputStream == null)
                    return false;

                using RentedArray<byte> bytes = RentedArray<byte>.Rent(4096);
                int read;

                dataStream.Seek(0, SeekOrigin.Begin);

                while ((read = dataStream.Read(bytes.Span)) > 0)
                    outputStream.Write(bytes.Span[..read]);
            }

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);

            filesystem.RemapFile(localInputFile, localOutputFile);
            pipeline.Associator.MakeAssocation(id, textureAssetId, true);

            AssetDatabase database = EditorRuntime.GlobalSingleton.AssetDatabase;
            database.AddEntry<TextureAtlasAsset>(new AssetDatabaseEntry(id, localInputFile, true));

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetDatabase database = EditorRuntime.GlobalSingleton.AssetDatabase;
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);

            database.AddEntry<TextureAtlasAsset>(new AssetDatabaseEntry(id, localFilePath, ValidateFile(localFilePath, filesystem, pipeline)));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);
            if (stream == null || stream.Length < Unsafe.SizeOf<TexAtlasHeader>())
                return false;

            TexAtlasHeader header = stream.Read<TexAtlasHeader>();
            return header.FileHeader == TexAtlasHeader.Header && header.FileVersion == TexAtlasHeader.Version;
        }

        public string? CustomFileIcon => throw new NotImplementedException();

        private static readonly TomlSerializerOptions s_serializerOptions = new TomlSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    internal sealed class TextureAtlasConfiguration
    {
        [TomlRequired] public string Texture { get; set; } = string.Empty;
        [TomlRequired] public TextureAtlasSprite[] Sprites { get; set; } = [];
    }

    internal sealed class TextureAtlasSprite
    {
        public string Name { get; set; } = string.Empty;
        [TomlConverter(typeof(Int2TomlConverter))] public Int2 Offset { get; set; } = Int2.Zero;
        [TomlConverter(typeof(Int2TomlConverter))] public Int2 Size { get; set; } = Int2.Zero;
    }

    //[TomlSourceGenerationOptions(Converters = [
    //    typeof(Int2TomlConverter),
    //    typeof(AssetIdTomlConverter)
    //    ], PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    //[TomlSerializable(typeof(TextureAtlasConfiguration))]
    //[TomlSerializable(typeof(TextureAtlasSprite))]
    //internal partial class TextureAtlasTomlContext : TomlSerializerContext
    //{
    //}
}
