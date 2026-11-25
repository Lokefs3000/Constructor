using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Importers;
using Tomlyn.Serialization;

namespace VoxelizationDemo.Assets.Importers
{
    [AssetImporter(".fxvol")]
    internal sealed class EffectVolumeImporter : IAssetImporter
    {
        public void ImportFile(in ImportContext context)
        {
            // Phew this was complicated
            context.InputStream.CopyTo(context.OutputStream);
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            using Stream? stream = FilesystemManager.OpenStream(localPath);

            if (stream == null || stream.Length < Unsafe.SizeOf<FileModelHeader>())
                return false;

            try
            {
                using var _ = JsonDocument.Parse(stream);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string UniqueId => "fxvolume";

        public Type AssetDefinitionType => typeof(EffectVolumeAsset);

        public string? DefaultConfigName => null;
        public Type? ConfigType => null;

        public TomlConverter[] Converters => [];
    }
}
