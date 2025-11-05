using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Editor.Serialization.Json
{
    internal class AssetJsonConverter : JsonConverter<IAssetDefinition>
    {
        public override void Write(Utf8JsonWriter writer, IAssetDefinition value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Id);
        }

        public override IAssetDefinition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!reader.Read() && reader.TokenType != JsonTokenType.Number)
                return null;
            return AssetManager.LoadAsset(typeToConvert, new AssetId(reader.GetUInt32())) as IAssetDefinition;
        }
    }

    internal class TextureJsonConverter : JsonConverter<TextureAsset>
    {
        public override void Write(Utf8JsonWriter writer, TextureAsset value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Id);
        }

        public override TextureAsset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!reader.Read() && reader.TokenType != JsonTokenType.Number)
                return null;
            return AssetManager.LoadAsset<TextureAsset>(new AssetId(reader.GetUInt32()));
        }
    }
}
