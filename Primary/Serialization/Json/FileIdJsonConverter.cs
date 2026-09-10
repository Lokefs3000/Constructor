using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Primary.Assets.Types;

namespace Primary.Serialization.Json
{
    public sealed class FileIdJsonConverter : JsonConverter<FileId>
    {
        public FileIdJsonConverter()
        {
        }

        public override FileId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string");

            if (reader.TryGetGuid(out Guid result))
                return new FileId(result);
            else
                throw new JsonException("Failed to parse file id");
        }

        public override void Write(Utf8JsonWriter writer, FileId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Guid);
        }
    }
}
