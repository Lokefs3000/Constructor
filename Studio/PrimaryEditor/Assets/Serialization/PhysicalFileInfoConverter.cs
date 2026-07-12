using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Primary.Assets.Types;
using Primary.Common;

namespace PrimaryEditor.Assets.Serialization
{
    internal class PhysicalFileInfoConverter : JsonConverter<KeyValuePair<AssetId, PhysicalFileInfo>>
    {
        public override KeyValuePair<AssetId, PhysicalFileInfo> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start object");
            reader.Read();

            AssetId id = AssetId.Invalid;
            DateTime lastModifiedTime = DateTime.MinValue;
            long fileSize = 0;

            int foundMask = 0;

            for (int i = 0; i < 4; i++)
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
                else if (reader.ValueTextEquals("lastModifiedTime"))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number)
                        throw new JsonException("Expected number for LastModifiedTime");
                    if (Flags.HasFlag(foundMask, 1 << 1))
                        throw new JsonException("LastModifiedTime is already defined");

                    if (long.TryParse(reader.ValueSpan, out long result))
                        lastModifiedTime = new DateTime(result);
                    else
                        throw new JsonException("Failed to parse LastModifiedTime");

                    foundMask |= 1 << 1;
                }
                else if (reader.ValueTextEquals("fileSize"))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number)
                        throw new JsonException("Expected number for FileSize");
                    if (Flags.HasFlag(foundMask, 1 << 2))
                        throw new JsonException("FileSize is already defined");

                    if (long.TryParse(reader.ValueSpan, out long result))
                        fileSize = result;
                    else
                        throw new JsonException("Failed to parse FileSize");

                    foundMask |= 1 << 2;
                }
                else if (reader.ValueTextEquals("checksum"))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException("Expected string for Checksum");
                    if (Flags.HasFlag(foundMask, 1 << 3))
                        throw new JsonException("Checksum is already defined");

                    foundMask |= 1 << 3;
                }
                else
                    throw new JsonException($"Unknown property name '{reader.GetString()}'");

                reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException("Expected end object");

            if (foundMask != 0b1111)
                throw new JsonException("Not all properties defined");

            return default;
        }

        public override void Write(Utf8JsonWriter writer, KeyValuePair<AssetId, PhysicalFileInfo> value, JsonSerializerOptions options)
        {
        }
    }
}
