using Arch.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Primary.Scenes.Json.Converters
{
    internal static class QuaternionConverter
    {
        public static bool TryDeserialize(ref Utf8JsonReader reader, ref Quaternion result)
        {
            Unsafe.SkipInit(out result);

            if (reader.TokenType != JsonTokenType.StartArray)
                goto ReturnBad;

            for (int i = 0; i < 4; ++i)
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.Number)
                    goto ReturnBad;

                if (reader.TryGetSingle(out float value))
                    result[i] = value;
                else
                    goto ReturnBad;
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                return false;

            result = Quaternion.Normalize(result);
            return true;
        ReturnBad:

            reader.TrySkip();
            return false;
        }
    }
}
