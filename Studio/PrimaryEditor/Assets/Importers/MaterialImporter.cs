using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Serialization.Toml;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Processors.Model;
using Silk.NET.Assimp;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Importers
{
    public sealed class MaterialImporter : IAssetImporter
    {
        public void ImportFile(in ImportContext context)
        {
            MaterialTomlOutput toml;
            try
            {
                toml = TomlSerializer.Deserialize<MaterialTomlOutput>(context.InputStream, MaterialTomlSerializerContext2.Default)!;
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "[{file}]: Error occured parsing material data", context.LocalPath);
                throw new AssetImportException("Error", ex);
            }

            if (context.Pipeline.AssetRegistry.IsIdValid(toml.Shader))
            {
                if (!context.Pipeline.AssetRegistry.TryGetLocalPathForId(toml.Shader, out string? localShaderPath))
                {
                    EdLog.Assets.Error("[{file}]: Failed to retrieve local path for shader id {id}", context.LocalPath, toml.Shader);
                    throw new AssetImportException();
                }

                if (!context.Pipeline.ImporterRegistry.TryGetImporterForPath(localShaderPath, out AssetImporterData importerData) || importerData.Importer is not ShaderImporter)
                {
                    EdLog.Assets.Error("[{file}]: Material shader has an incorrect or missing type '{id}'", context.LocalPath, importerData.Importer?.UniqueId ?? "<null>");
                    throw new AssetImportException();
                }

                context.AddReloadConnection(toml.Shader);
            }

            context.InputStream.Seek(0, SeekOrigin.Begin);
            context.InputStream.CopyTo(context.OutputStream);
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            return true;
        }

        public string UniqueId => "material";
        public Type AssetDefinitionType => typeof(MaterialAsset);

        public string? DefaultConfigName => "DefaultConfig_Material.toml";
        public Type? ConfigType => null;

        public TomlConverter[] Converters => [];
    }

    [TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, Converters = [
        typeof(EarlyAssetIdTomlConverter)
        ])]
    [TomlSerializable(typeof(MaterialTomlOutput))]
    public partial class MaterialTomlSerializerContext2 : TomlSerializerContext
    {
    }
}
