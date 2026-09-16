using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Serialization.Json;

namespace VoxelizationDemo.Editor.Serialization.Converters
{
    internal sealed class AssetDefinitionTypeConverter : TypeConverter<IAssetDefinition>
    {
        public override void DeserializeToProperty(ref Utf8JsonReader reader, ValueSource valueSource, object boxed)
        {
            AssetId id = s_assetIdJsonConverter.Read(ref reader, valueSource.TargetType, JsonSerializerOptions.Default);
            if (!id.IsInvalid)
            {
                valueSource.Set(boxed, (IAssetDefinition?)AssetManager.LoadAsset(valueSource.TargetType, id));
            }
        }

        public override void SerializeToWriter(Utf8JsonWriter writer, ValueSource valueSource, object boxed)
        {
            IAssetDefinition? asset = valueSource.Get<IAssetDefinition>(boxed);

            if (asset == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                s_assetIdJsonConverter.Write(writer, asset.Id, JsonSerializerOptions.Default);
            }
        }

        private static readonly AssetIdJsonConverter s_assetIdJsonConverter = new AssetIdJsonConverter();
    }
}
