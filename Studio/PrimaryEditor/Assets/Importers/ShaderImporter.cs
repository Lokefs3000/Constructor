using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Editor.Shaders;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Serialization.Toml;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Processors.Shader;
using PrimaryEditor.Project;
using Tomlyn;
using ShaderProcessor = PrimaryEditor.Processors.Shader.ShaderProcessor;

namespace PrimaryEditor.Assets.Importers
{
    public sealed class ShaderImporter : IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, Stream outputStream, string localPath, string localOutputPath, bool isTrialImport)
        {
            pipeline.FilesystemManager.SetFileRemap(localPath, null);

            string? configFile = pipeline.Configuration.GetLocalConfigPath(localPath);
            if (configFile == null)
            {
                pipeline.ReportImportAsIgnored(id, this);
                throw new AssetIgnoredException();
            }

            string? sourceText = FilesystemManager.ReadAllText(configFile);
            if (sourceText == null)
            {
                EdLog.Assets.Error("[{file}]: Failed to read shader configuration", localPath);
                throw new AssetImportException();
            }

            ShaderConfiguration config;
            try
            {
                config = TomlSerializer.Deserialize<ShaderConfiguration>(sourceText, s_tomlOptions)!;
            }
            catch (TomlException ex)
            {
                if (isTrialImport)
                    throw new AssetIgnoredException();

                EdLog.Assets.Error(ex, "[{file}]: Error occured parsing shader configuration", localPath);
                throw new AssetImportException();
            }

            config.DefaultId = id;
            config.IdProvider = pipeline.AssetRegistry;

            config.IncludeDirectories = [
                Path.GetDirectoryName(FilesystemManager.GetFullPath(localPath)!)!,
                .. pipeline.FilesystemManager.Filesystems
                    .Where(static (x) => x is ContentFilesystem)
                    .Select(static (x) => Path.GetDirectoryName(x.WorkingDirectory) ?? x.WorkingDirectory)];

            ShaderProcesserResult result;
            try
            {
                result = ShaderProcessor.Execute(config, ShaderCompileTarget.Direct3D12, outputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Shader processor encountered an error");
                throw new AssetLoadException();
            }

            if (result.IncludedFiles.Length == 0)
            {
                pipeline.Associator.ClearAssociations(id);
            }
            else
            {
                using RentedList<AssetId> includedFileIds = new RentedList<AssetId>();
                foreach (string includedFile in result.IncludedFiles)
                {
                    if (FilesystemManager.TryGetLocalPath(includedFile, out string? includeLocalPath))
                    {
                        includedFileIds.Add(pipeline.GetOrRegisterIdForPath(includeLocalPath));
                    }
                }

                pipeline.Associator.MakeAssociations(id, includedFileIds.AsSpan(), true);
            }

            pipeline.FilesystemManager.SetFileRemap(localPath, localOutputPath);
            pipeline.ReloadAsset(id);
        }

        public void PreloadFile(AssetPipeline pipeline, AssetId id)
        {
            throw new NotImplementedException();
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            string? configFile = pipeline.Configuration.GetConfigPath(localPath);
            if (configFile == null)
                return false;

            string? sourceFile = FilesystemManager.ReadAllText(configFile);
            if (sourceFile == null)
                return false;
            if (!TomlSerializer.TryDeserialize<ShaderConfiguration>(sourceFile, out _, s_tomlOptions))
                return false;

            using Stream? stream = FilesystemManager.OpenStream(localPath);

            if (stream == null || stream.Length < Unsafe.SizeOf<SBCHeader>())
                return false;

            SBCHeader header = stream.Read<SBCHeader>();

            if (header.Header != SBCHeader.ConstHeader) return false;
            if (header.Version != SBCHeader.ConstVersion) return false;

            return true;
        }

        public string UniqueId => "shader";

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            Converters = [
                new EarlyAssetIdTomlConverter()
                ],
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
