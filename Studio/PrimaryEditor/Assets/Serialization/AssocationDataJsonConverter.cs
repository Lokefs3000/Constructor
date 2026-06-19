using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;

namespace PrimaryEditor.Assets.Serialization
{
    internal class AssocationDataJsonConverter : JsonConverter<KeyValuePair<AssetId, AssetId[]>>
    {
        public override KeyValuePair<AssetId, AssetId[]> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected object start");
            reader.Read();

            AssetId id = AssetId.Invalid;
            RentedList<AssetId> dependencies = new RentedList<AssetId>();

            int foundMask = 0;

            for (int i = 0; i < 2; i++)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name");

                if (reader.ValueTextEquals("id"))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException("Expected string for Id");
                    if (Flags.HasFlag(foundMask, 1 << 0))
                        throw new JsonException("Id is already defined");

                    if (Guid.TryParse(reader.ValueSpan, out Guid result))
                        id = new AssetId(result);
                    else
                        throw new JsonException("Failed to parse Id");

                    foundMask |= 1 << 0;
                }
                else if (reader.ValueTextEquals("dependencies"))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected array for Dependencies");
                    if (Flags.HasFlag(foundMask, 1 << 1))
                        throw new JsonException("Dependencies is already defined");

                    while (true)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;

                        if (reader.TokenType != JsonTokenType.String)
                            throw new JsonException("Expected string for dependency id");

                        if (Guid.TryParse(reader.ValueSpan, out Guid result))
                            dependencies.Add(new AssetId(result));
                        else
                            throw new JsonException("Failed to parse dependency Id");
                    }

                    foundMask |= 1 << 1;
                }
                else
                    throw new JsonException($"Unknown property name '{reader.GetString()}'");

                reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException("Expected object end");

            if (foundMask != 0b11)
                throw new JsonException("Not all properties defined");

            return new KeyValuePair<AssetId, AssetId[]>(id, dependencies.ToArray());
        }

        public override void Write(Utf8JsonWriter writer, KeyValuePair<AssetId, AssetId[]> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.Key.ToString());

            writer.WriteStartArray("dependencies");

            foreach (AssetId dependency in value.Value)
            {
                writer.WriteStringValue(dependency.ToString());
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
