using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using Primary.Serialization.Toml;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Processors.Shader;
using PrimaryEditor.Processors.UIFontFamily;
using Tomlyn;

namespace PrimaryEditor.Assets.Importers
{
    internal sealed class UIFontFamilyImporter : IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, Stream outputStream, string localPath, string localOutputPath, bool isTrialImport)
        {
            pipeline.FilesystemManager.SetFileRemap(localPath, null);

            string? sourceText = FilesystemManager.ReadAllText(localPath);
            if (sourceText == null)
            {
                EdLog.Assets.Error("[{file}]: Failed to read font data", localPath);
                throw new AssetImportException();
            }

            UIFontFamilyConfiguration config;
            try
            {
                config = TomlSerializer.Deserialize<UIFontFamilyConfiguration>(sourceText, s_tomlOptions)!;
            }
            catch (TomlException ex)
            {
                EdLog.Assets.Error(ex, "[{file}]: Error occured parsing font data", localPath);
                throw new AssetImportException();
            }

            config.IdProvider = pipeline.AssetRegistry;

            try
            {
                UIFontFamilyProcessor.Execute(config, outputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Font processor encountered an error");
                throw new AssetImportException();
            }

            using RentedList<AssetId> ids = new RentedList<AssetId>();
            foreach (UIFFCFont font in config.Fonts)
            {
                ids.Add(font.Source);
            }

            pipeline.Associator.MakeAssociations(id, ids.AsSpan(), true);
            pipeline.FilesystemManager.SetFileRemap(localPath, localOutputPath);
            pipeline.ReloadAsset(id);
        }

        public void PreloadFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            using Stream? stream = FilesystemManager.OpenStream(localPath);

            if (stream == null || stream.Length < Unsafe.SizeOf<UIFontFamilyHeader>())
                return false;

            UIFontFamilyHeader header = stream.Read<UIFontFamilyHeader>();

            if (header.FileHeader != UIFontFamilyHeader.Header) return false;
            if (header.FileVersion != UIFontFamilyHeader.Version) return false;

            return true;
        }

        public string UniqueId => "eui_fontfamily";
        public Type AssetDefinitionType => typeof(UIFontFamilyAsset);

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            Converters = [
                new EarlyAssetIdTomlConverter()
                ],
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
