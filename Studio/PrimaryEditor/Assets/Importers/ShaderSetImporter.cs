using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Utility;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Utility;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Importers
{
    public sealed class ShaderSetImporter : IAssetImporter
    {
        public void ImportFile(in ImportContext context)
        {
            Dictionary<string, NonFavoredAssetId> set;
            try
            {
                set = TomlSerializer.Deserialize<Dictionary<string, NonFavoredAssetId>>(context.InputStream, s_tomlOptions)!;
            }
            catch (Exception ex)
            {
                throw new AssetImportException("Failed to deserialize toml", ex);
            }

            if (set.Count > byte.MaxValue)
            {
                EdLog.Assets.Warning("[{file}]: Shader set has more entries than the supported 255 (count: {c})", context.LocalPath, set.Count);
            }

            context.OutputStream.Write(new FileShaderSetHeader
            {
                FileHeader = FileShaderSetHeader.Header,
                FileVersion = FileShaderSetHeader.Version,
                ShaderCount = (byte)Math.Min(set.Count, byte.MaxValue)
            });

            HashSet<string>? trimCollisionSet = null;
            foreach (var (name, favoredId) in set)
            {
                AssetId id = favoredId.Id;

                if (!context.Pipeline.AssetRegistry.IsIdValid(id))
                {
                    throw new AssetLoadException($"Shader id '{id}' is not valid");
                }

                if (context.Pipeline.AssetRegistry.TryGetLocalPathForId(id, out string? value) &&
                    context.Pipeline.ImporterRegistry.TryGetImporterForPath(value, out AssetImporterData importerData))
                {
                    if (importerData.Importer is not ShaderImporter)
                    {
                        throw new AssetLoadException($"Id '{id}' does not refer to a shader file");
                    }
                }
                else
                {
                    EdLog.Assets.Warning("[{file}]: Failed to validate what asset id refers to", context.LocalPath);
                }

                string truncatedName = name;
                if (truncatedName.Length > byte.MaxValue)
                {
                    EdLog.Assets.Warning("[{file}]: Shader set entry name is longer than max of 255 and will be truncated (length: {l})", name);
                    truncatedName = truncatedName[..byte.MaxValue];

                    if (!(trimCollisionSet ??= new HashSet<string>()).Add(truncatedName))
                    {
                        throw new AssetLoadException($"Truncated entry name '{truncatedName}' resulted in collision");
                    }
                }

                context.OutputStream.Write(new FileShaderEntry
                {
                    NameLength = (byte)truncatedName.Length,
                    Id = id
                });

                context.OutputStream.Write(Encoding.UTF8.GetBytes(truncatedName));
            }
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            return true;
        }

        public string UniqueId => "shaderset";
        public Type AssetDefinitionType => typeof(ShaderSetAsset);

        public string? DefaultConfigName => null;
        public Type? ConfigType => null;

        public TomlConverter[] Converters => [];

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            Converters = [
                NonFavoredAssetId.EarlyConverter.Instance
                ]
        };
    }
}
