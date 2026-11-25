using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Editor.Shaders;
using Primary.Assets;
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
using Tomlyn.Serialization;
using ShaderProcessor = PrimaryEditor.Processors.Shader.ShaderProcessor;

namespace PrimaryEditor.Assets.Importers
{
    public sealed class ShaderImporter : IAssetImporter
    {
        public void ImportFile(in ImportContext context)
        {
            ShaderConfiguration config = context.GetAssetConfiguration<ShaderConfiguration>();

            config.DefaultId = context.Id;
            config.IdProvider = context.Pipeline.AssetRegistry;

            config.IncludeDirectories = [
                Path.GetDirectoryName(FilesystemManager.GetFullPath(context.LocalPath)!)!,
                .. context.Pipeline.FilesystemManager.Filesystems
                    .Where(static (x) => x is ContentFilesystem)
                    .Select(static (x) => Path.GetDirectoryName(x.WorkingDirectory) ?? x.WorkingDirectory)];

            ShaderProcesserResult result;
            try
            {
                result = ShaderProcessor.Execute(config, ShaderCompileTarget.Direct3D12, context.OutputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Shader processor encountered an error");
                throw new AssetLoadException();
            }

            foreach (string includedFile in result.IncludedFiles)
            {
                if (FilesystemManager.TryGetLocalPath(includedFile, out string? includeLocalPath))
                {
                    context.AddDependency(context.Pipeline.GetOrRegisterIdForPath(includeLocalPath));
                }
            }
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            using Stream? stream = FilesystemManager.OpenStream(localPath);

            if (stream == null || stream.Length < Unsafe.SizeOf<SBCHeader>())
                return false;

            SBCHeader header = stream.Read<SBCHeader>();

            if (header.Header != SBCHeader.ConstHeader) return false;
            if (header.Version != SBCHeader.ConstVersion) return false;

            return true;
        }

        public string UniqueId => "shader";
        public Type AssetDefinitionType => typeof(ShaderAsset);

        public string? DefaultConfigName => "DefaultConfig_Shader.toml";
        public Type? ConfigType => typeof(ShaderConfiguration);

        public TomlConverter[] Converters => [];

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            Converters = [
                new EarlyFileIdTomlConverter()
                ],
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
