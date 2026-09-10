using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Editor.Shaders;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Collections;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Processors.ComputeShader;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Importers
{
    public sealed class ComputeShaderImporter : IAssetImporter
    {
        public void ImportFile(in ImportContext context)
        {
            ComputeShaderConfiguration config = context.GetAssetConfiguration<ComputeShaderConfiguration>();

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
                result = ComputeShaderProcessor.Execute(config, ShaderCompileTarget.Direct3D12, context.OutputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Compute shader processor encountered an error");
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

            if (stream == null || stream.Length < Unsafe.SizeOf<CBCHeader>())
                return false;

            CBCHeader header = stream.Read<CBCHeader>();

            if (header.Header != CBCHeader.ConstHeader) return false;
            if (header.Version != CBCHeader.ConstVersion) return false;

            return true;
        }

        public string UniqueId => "compute_shader";
        public Type AssetDefinitionType => typeof(ComputeShaderAsset);

        public string? DefaultConfigName => "DefaultConfig_ComputeShader.toml";
        public Type? ConfigType => typeof(ComputeShaderConfiguration);

        public TomlConverter[] Converters => [];

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            Converters = [
                new EarlyAssetIdTomlConverter()
                ],
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
