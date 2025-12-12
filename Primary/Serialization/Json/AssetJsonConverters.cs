using Primary.Assets;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Primary.Serialization.Json
{
    internal class TextureAssetJsonConverter : JsonConverter<TextureAsset>
    {
        public override TextureAsset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt32(out uint assetId))
            {
                return AssetManager.LoadAsset<TextureAsset>((AssetId)assetId);
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, TextureAsset value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Id);
        }
    }
}
