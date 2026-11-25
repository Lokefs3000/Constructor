using CommunityToolkit.HighPerformance;
using Editor.Storage;
using Editor.UI.Assets;
using Editor.UI.Assets.Importers;
using Editor.UI.Assets.Loaders;
using Editor.UI.Menu;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Assets.Importers
{
    internal sealed class ContextMenuAssetImporter : IAssetImporter
    {
        public ContextMenuAssetImporter()
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

            FileContextMenuRoot? root = JsonSerializer.Deserialize(stream, FileContextMenuSerializer.Default.FileContextMenuRoot);
            if (root == null)
                return false;

            AssetId fontAssetId;
            {
                if (Guid.TryParse(root.Font, out Guid result))
                    fontAssetId = new AssetId(result);
                else
                    fontAssetId = pipeline.Identifier.RetriveIdForPath(root.Font);

                if (!pipeline.Identifier.IsIdValid(fontAssetId))
                    return false;
            }

            string? fontPath = pipeline.Identifier.RetrievePathForId(fontAssetId);
            if (fontPath == null || !pipeline.TryGetImporter(fontPath, out IAssetImporter? importer) || importer is not UIFontAssetImporter)
                return false;

            if (root.Items.Length > byte.MaxValue)
                return false;

            MemoryStream dataStream = new MemoryStream();

            dataStream.Write(new CtxMenuHeader
            {
                FileHeader = CtxMenuHeader.Header,
                FileVersion = CtxMenuHeader.Version,

                FontAssetId = fontAssetId,

                ItemCount = (byte)root.Items.Length
            });

            foreach (FileContextMenuBase @base in root.Items)
            {
                if (!RecursiveItemSerialize(@base))
                    return false;
            }

            bool RecursiveItemSerialize(FileContextMenuBase @base)
            {
                ReadOnlySpan<char> idSpan = @base.Id == null ? ReadOnlySpan<char>.Empty : @base.Id.AsSpan();
                if (idSpan.Length > byte.MaxValue)
                {
                    EdLog.Assets.Warning("[{p}]: Context menu item id is longer than >{max} characters and will be trimmed {name}", localInputFile, byte.MaxValue, @base.Id);
                    idSpan = idSpan[..byte.MaxValue];
                }

                if (@base is FileContextMenuItem item)
                {
                    if (item.Items.Length > byte.MaxValue)
                        return false;

                    ReadOnlySpan<char> textSpan = item.Text == null ? ReadOnlySpan<char>.Empty : item.Text.AsSpan();
                    if (textSpan.Length > byte.MaxValue)
                    {
                        EdLog.Assets.Warning("[{p}]: Context menu item text is longer than >{max} characters and will be trimmed {name}", localInputFile, byte.MaxValue, item.Text);
                        textSpan = textSpan[..byte.MaxValue];
                    }

                    CtxMenuImageType imageType = CtxMenuImageType.None;
                    if (item.Image != null)
                    {
                        if (item.Image is FileContextMenuTextureAsset)
                            imageType = CtxMenuImageType.TextureAsset;
                        else if (item.Image is FileContextMenuSprite)
                            imageType = CtxMenuImageType.Sprite;
                    }

                    dataStream.Write(new CtxMenuBase
                    {
                        Type = CtxMenuType.Item,
                        IdLength = (byte)idSpan.Length
                    });

                    if (!idSpan.IsEmpty)
                        dataStream.Write(MemoryMarshal.Cast<char, byte>(idSpan));

                    dataStream.Write(new CtxMenuItem
                    {
                        TextLength = (byte)textSpan.Length,
                        ImageType = imageType,
                        ItemCount = (byte)item.Items.Length
                    });

                    if (!textSpan.IsEmpty)
                        dataStream.Write(MemoryMarshal.Cast<char, byte>(textSpan));

                    if (imageType != CtxMenuImageType.None)
                    {
                        if (imageType == CtxMenuImageType.TextureAsset)
                        {
                            FileContextMenuTextureAsset image = (FileContextMenuTextureAsset)item.Image!;

                            AssetId textureAssetId;
                            {
                                if (Guid.TryParse(image.AssetId, out Guid result))
                                    textureAssetId = new AssetId(result);
                                else
                                    textureAssetId = pipeline.Identifier.RetriveIdForPath(image.AssetId);

                                if (!pipeline.Identifier.IsIdValid(textureAssetId))
                                    return false;
                            }

                            string? texturePath = pipeline.Identifier.RetrievePathForId(textureAssetId);
                            if (texturePath == null || !pipeline.TryGetImporter(texturePath, out IAssetImporter? importer) || importer is not TextureAssetImporter)
                                return false;

                            dataStream.Write(new CtxMenuTextureAsset
                            {
                                Asset = textureAssetId
                            });
                        }
                        else if (imageType == CtxMenuImageType.Sprite)
                        {
                            FileContextMenuSprite image = (FileContextMenuSprite)item.Image!;

                            AssetId atlasAssetId;
                            {
                                if (Guid.TryParse(image.AssetId, out Guid result))
                                    atlasAssetId = new AssetId(result);
                                else
                                    atlasAssetId = pipeline.Identifier.RetriveIdForPath(image.AssetId);

                                if (!pipeline.Identifier.IsIdValid(atlasAssetId))
                                    return false;
                            }

                            string? atlasPath = pipeline.Identifier.RetrievePathForId(atlasAssetId);
                            if (atlasPath == null || !pipeline.TryGetImporter(atlasPath, out IAssetImporter? importer) || importer is not TextureAtlasAssetImporter)
                                return false;

                            ReadOnlySpan<char> spriteSpan = image.SpriteName.AsSpan();
                            if (spriteSpan.Length > byte.MaxValue)
                            {
                                EdLog.Assets.Warning("[{p}]: Sprite name is longer than >{max} characters and will be trimmed {name}", localInputFile, byte.MaxValue, image.SpriteName);
                                spriteSpan = spriteSpan[..byte.MaxValue];
                            }

                            dataStream.Write(new CtxMenuSprite
                            {
                                Asset = atlasAssetId,
                                SpriteNameLength = (byte)spriteSpan.Length
                            });

                            if (!spriteSpan.IsEmpty)
                                dataStream.Write(MemoryMarshal.Cast<char, byte>(spriteSpan));
                        }
                    }

                    foreach (FileContextMenuBase childItem in item.Items)
                    {
                        RecursiveItemSerialize(childItem);
                    }
                }
                else if (@base is FileContextMenuSeparator)
                {
                    dataStream.Write(new CtxMenuBase
                    {
                        Type = CtxMenuType.Separator,
                        IdLength = (byte)idSpan.Length
                    });

                    if (!idSpan.IsEmpty)
                        dataStream.Write(MemoryMarshal.Cast<char, byte>(idSpan));
                }

                return true;
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
            
            AssetDatabase database = EditorRuntime.GlobalSingleton.AssetDatabase;
            database.AddEntry<ContextMenuAsset>(new AssetDatabaseEntry(id, localInputFile, true));

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetDatabase database = EditorRuntime.GlobalSingleton.AssetDatabase;
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);

            database.AddEntry<ContextMenuAsset>(new AssetDatabaseEntry(id, localFilePath, ValidateFile(localFilePath, filesystem, pipeline)));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);
            if (stream == null || stream.Length < Unsafe.SizeOf<CtxMenuHeader>())
                return false;

            CtxMenuHeader header = stream.Read<CtxMenuHeader>();
            return header.FileHeader == CtxMenuHeader.Header && header.FileVersion == CtxMenuHeader.Version;
        }

        public string? CustomFileIcon => throw new NotImplementedException();
    }

    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(FileContextMenuRoot))]
    [JsonSerializable(typeof(FileContextMenuItem))]
    [JsonSerializable(typeof(FileContextMenuSeparator))]
    [JsonSerializable(typeof(FileContextMenuImage))]
    internal partial class FileContextMenuSerializer : JsonSerializerContext
    {
    }

    internal sealed class FileContextMenuRoot
    {
        [JsonRequired] public string Font { get; set; } = string.Empty;
        public string[] Stylesheets { get; set; } = [];
        public FileContextMenuBase[] Items { get; set; } = [];
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
    [JsonDerivedType(typeof(FileContextMenuItem), "Item")]
    [JsonDerivedType(typeof(FileContextMenuSeparator), "Separator")]
    internal abstract class FileContextMenuBase
    {
        public string? Id { get; set; } = string.Empty;
    }

    internal sealed class FileContextMenuItem : FileContextMenuBase
    {
        public string? Text { get; set; } = null;
        public FileContextMenuImage? Image { get; set; } = null;
        public FileContextMenuBase[] Items { get; set; } = [];
    }

    internal sealed class FileContextMenuSeparator : FileContextMenuBase
    {
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(FileContextMenuTextureAsset))]
    [JsonDerivedType(typeof(FileContextMenuSprite))]
    [JsonConverter(typeof(ThisConverter))]
    internal abstract class FileContextMenuImage
    {
        internal sealed class ThisConverter : JsonConverter<FileContextMenuImage>
        {
            public override FileContextMenuImage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return new FileContextMenuTextureAsset { AssetId = reader.GetString() ?? throw new JsonException() };
                }
                else if (reader.TokenType == JsonTokenType.StartArray)
                {
                    reader.Read();
                    string assetId = reader.GetString() ?? throw new JsonException();

                    reader.Read();
                    string spriteName = reader.GetString() ?? throw new JsonException();

                    reader.Read();
                    if (reader.TokenType != JsonTokenType.EndArray)
                        throw new JsonException();

                    return new FileContextMenuSprite { AssetId = assetId, SpriteName = spriteName };
                }
                else
                    throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, FileContextMenuImage value, JsonSerializerOptions options)
            {
                if (value is FileContextMenuTextureAsset textureAsset)
                    writer.WriteStringValue(textureAsset.AssetId);
                else if (value is FileContextMenuSprite sprite)
                {
                    writer.WriteStartArray();
                    writer.WriteStringValue(sprite.AssetId);
                    writer.WriteStringValue(sprite.SpriteName);
                    writer.WriteEndArray();
                }
                else
                    throw new JsonException();
            }
        }
    }

    internal class FileContextMenuTextureAsset : FileContextMenuImage
    {
        public string AssetId { get; set; } = string.Empty;
    }

    internal class FileContextMenuSprite : FileContextMenuImage
    {
        public string AssetId { get; set; } = string.Empty;
        public string SpriteName { get; set; } = string.Empty;
    }
}
