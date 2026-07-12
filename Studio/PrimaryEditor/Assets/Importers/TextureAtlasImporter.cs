using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Processors.TextureAtlas;
using Tomlyn;

namespace PrimaryEditor.Assets.Importers
{
    internal sealed class TextureAtlasImporter : IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, Stream outputStream, string localPath, string localOutputPath, bool isTrialImport)
        {
            pipeline.FilesystemManager.SetFileRemap(localPath, null);

            string? sourceText = FilesystemManager.ReadAllText(localPath);
            if (sourceText == null)
            {
                EdLog.Assets.Error("[{file}]: Failed to read texture atlas source", localPath);
                throw new AssetImportException();
            }

            TextureAtlasConfiguration config;
            try
            {
                config = TomlSerializer.Deserialize<TextureAtlasConfiguration>(sourceText, s_tomlOptions)!;
            }
            catch (TomlException ex)
            {
                EdLog.Assets.Error(ex, "[{file}]: Error occured parsing texture atlas", localPath);
                throw new AssetImportException();
            }

            try
            {
                TextureAtlasProcessor.Execute(localPath, config, outputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Texture atlas processor encountered an error");
                throw new AssetImportException();
            }

            pipeline.Associator.MakeAssociation(id, config.Texture, true);

            pipeline.FilesystemManager.SetFileRemap(localPath, localOutputPath);
            pipeline.ReloadAsset(id);
        }

        public void PreloadFile(AssetPipeline pipeline, AssetId id)
        {
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

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            Converters = [
                new EarlyAssetIdTomlConverter(),
               ],
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
