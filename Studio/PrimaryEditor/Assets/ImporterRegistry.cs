using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Primary.Common;
using PrimaryEditor.Assets.Importers;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Project;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets
{
    public sealed class ImporterRegistry
    {
        private Dictionary<string, AssetImporterData> _importers;
        private Dictionary<string, AssetImporterData>.AlternateLookup<ReadOnlySpan<char>> _altImporterLookup;

        internal ImporterRegistry()
        {
            _importers = new Dictionary<string, AssetImporterData>();
            _altImporterLookup = _importers.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public bool TryGetImporterFor(ReadOnlySpan<char> extension, [NotNullWhen(true)] out AssetImporterData importerData)
        {
            return _altImporterLookup.TryGetValue(extension, out importerData);
        }

        public bool TryGetImporterForPath(ReadOnlySpan<char> path, [NotNullWhen(true)] out AssetImporterData importerData)
        {
            int index = path.LastIndexOf('.');
            if (index == -1)
            {
                importerData = default;
                return false;
            }

            return _altImporterLookup.TryGetValue(path[index..], out importerData);
        }

        internal void RegisterImporter<T>(params string[] extensions) where T : class, IAssetImporter, new()
        {
            IAssetImporter importer = new T();

            TomlPolymorphismOptions? polymorphismOptions = null;

            Type? customConfigType = importer.ConfigType;
            object? defaultConfig = null;

            TomlConverter[] additionalConverters = importer.Converters;

            if (customConfigType != null)
            {
                polymorphismOptions = new TomlPolymorphismOptions
                {
                    DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
                    {
                        { typeof(object), [ new TomlDerivedType(customConfigType) ] }
                    },
                    UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.FallBackToBaseType,
                };

                string? defaultConfigName = importer.DefaultConfigName;
                if (defaultConfigName != null)
                {
                    TomlSerializerOptions tempOptions = additionalConverters.Length > 0 ? new TomlSerializerOptions
                    {
                        WriteIndented = true,
                        IndentSize = 4,
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        Converters = [
                            new EarlyFileIdTomlConverter(),
                            new EarlyAssetIdTomlConverter(),
                            .. additionalConverters
                        ]
                    } : s_tomlOptions;

                    string fullPath = Path.Combine(ProjectData.Instance!.Paths.PrefsFolder, defaultConfigName);
                    try
                    {
                        using Stream? stream = FileUtility.TryWaitOpen(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 2, timeoutMs: 20);
                        defaultConfig = TomlSerializer.Deserialize(stream, customConfigType, tempOptions);
                    }
                    catch (Exception ex)
                    {
                        EdLog.Assets.Warning(ex, "Failed to parse custom config with name '{n}'", defaultConfigName);
                    }
                }
            }

            TomlSerializerOptions serializerOptions = new TomlSerializerOptions
            {
                WriteIndented = true,
                IndentSize = 4,
                PolymorphismOptions = polymorphismOptions ?? new TomlPolymorphismOptions(),
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                Converters = [
                    new EarlyFileIdTomlConverter(),
                    new EarlyAssetIdTomlConverter(),
                    .. additionalConverters
                ]
            };

            foreach (string extension in extensions)
            {
                if (!_importers.TryAdd(extension, new AssetImporterData(importer, serializerOptions, defaultConfig)))
                {
                    EdLog.Assets.Warning("Cannot register asset importer '{t}' because a diffent importer was already registered to the file extension '{ex}'", typeof(T), extension);
                }
            }
        }

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 4,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = [
                    new EarlyFileIdTomlConverter(),
                    new EarlyAssetIdTomlConverter()
                ]
        };
    }

    public readonly record struct AssetImporterData(IAssetImporter Importer, TomlSerializerOptions TomlOptions, object? DefaultConfig);
}
