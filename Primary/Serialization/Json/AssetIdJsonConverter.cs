using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Primary.Assets.Types;

namespace Primary.Serialization.Json
{
    public sealed class AssetIdJsonConverter : JsonConverter<AssetId>
    {
        public AssetIdJsonConverter()
        {
        }

        public override AssetId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string");

            if (Guid.TryParse(reader.ValueSpan, out Guid result))
                return new AssetId(result);
            else
                throw new JsonException("Failed to parse asset id");
        }

        public override void Write(Utf8JsonWriter writer, AssetId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
