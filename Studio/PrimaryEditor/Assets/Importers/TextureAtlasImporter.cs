using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Utility;
using PrimaryEditor.Assets.Database;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Processors.TextureAtlas;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Importers
{
    internal sealed class TextureAtlasImporter : IAssetImporter
    {
        public void ImportFile(in ImportContext context)
        {
            TextureAtlasConfiguration config;
            try
            {
                config = TomlSerializer.Deserialize<TextureAtlasConfiguration>(context.InputStream, s_tomlOptions)!;
            }
            catch (TomlException ex)
            {
                EdLog.Assets.Error(ex, "[{file}]: Error occured parsing texture atlas", context.LocalPath);
                throw new AssetImportException();
            }

            try
            {
                TextureAtlasProcessor.Execute(context.LocalPath, config, context.OutputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Texture atlas processor encountered an error");
                throw new AssetImportException();
            }

            context.AddDependency(config.Texture);
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            using Stream? stream = FilesystemManager.OpenStream(localPath);

            if (stream == null || stream.Length < Unsafe.SizeOf<TexAtlasHeader>())
                return false;

            TexAtlasHeader header = stream.Read<TexAtlasHeader>();

            if (header.FileHeader != TexAtlasHeader.Header) return false;
            if (header.FileVersion != TexAtlasHeader.Version) return false;

            return true;
        }

        public string UniqueId => "texture_atlas";
        public Type AssetDefinitionType => typeof(TextureAtlasAsset);

        public string? DefaultConfigName => "DefaultConfig_TextureAtlas.toml";
        public Type? ConfigType => null;

        public TomlConverter[] Converters => [];

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            Converters = [
                new EarlyAssetIdTomlConverter(),
               ],
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
