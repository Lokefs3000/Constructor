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
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();

                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException("Expected GUID for asset id");
                if (!reader.TryGetGuid(out Guid guid))
                    throw new JsonException("Failed to parse GUID");

                reader.Read();

                if (reader.TokenType != JsonTokenType.Number)
                    throw new JsonException("Expected number id for local id");
                if (!reader.TryGetInt32(out int localId))
                    throw new JsonException("Failed to parse local id");

                reader.Read();

                if (reader.TokenType != JsonTokenType.EndArray)
                    throw new JsonException("Expected end of array for asset id");

                return new AssetId((FileId)guid, localId);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException("Expected GUID for asset id");
                if (!reader.TryGetGuid(out Guid guid))
                    throw new JsonException("Failed to parse GUID");

                return new AssetId((FileId)guid, 0);
            }
            else
            {
                throw new JsonException("Expected either array or GUID for asset id");
            }
        }

        public override void Write(Utf8JsonWriter writer, AssetId value, JsonSerializerOptions options)
        {
            if (value.LocalId != AssetId.NoLocalId)
            {
                writer.WriteStartArray();
                writer.WriteStringValue(value.FileId.Guid);
                writer.WriteNumberValue(value.LocalId);
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStringValue(value.FileId.Guid);
            }
        }
    }
}
