using Arch.Core;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Primary.Scenes.Json.Converters
{
    internal static class ColorConverter
    {
        public static bool TryDeserialize(ref Utf8JsonReader reader, ref Color result)
        {
            Unsafe.SkipInit(out result);

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetSingle(out float rgb))
                {
                    result = new Color(rgb);
                    return true;
                }
                else
                    goto ReturnBad;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
                goto ReturnBad;

            for (int i = 0; i < 4; ++i)
            {
                reader.Read();

                if (i == 2 && reader.TokenType == JsonTokenType.EndArray)
                {
                    result = new Color(result.R, result.G);
                    return true;
                }
                else if (reader.TokenType != JsonTokenType.Number)
                    goto ReturnBad;

                if (reader.TryGetSingle(out float value))
                    result[i] = value;
                else
                    goto ReturnBad;
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                return false;

            return true;
        ReturnBad:

            reader.TrySkip();
            return false;
        }

        public static bool TrySerialize(Utf8JsonWriter writer, ref Color value)
        {
            if (value.R == value.G && value.R == value.B)
            {
                if (value.A >= 1.0f)
                {
                    writer.WriteNumberValue(value.R);
                }
                else
                {
                    writer.WriteStartArray();
                    writer.WriteNumberValue(value.R);
                    writer.WriteNumberValue(value.A);
                    writer.WriteEndArray();
                }
            }
            else
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(value.R);
                writer.WriteNumberValue(value.G);
                writer.WriteNumberValue(value.B);
                writer.WriteNumberValue(value.A);
                writer.WriteEndArray();
            }

            return true;
        }
    }
}
