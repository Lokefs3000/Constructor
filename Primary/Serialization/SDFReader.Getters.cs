namespace Primary.Serialization
{
    public static class SDFReaderGetters
    {
        #region Get
        public static sbyte GetSByte(this SDFReader reader) => sbyte.Parse(reader.Slice);
        public static short GetInt16(this SDFReader reader) => short.Parse(reader.Slice);
        public static int GetInt32(this SDFReader reader) => int.Parse(reader.Slice);
        public static long GetInt64(this SDFReader reader) => long.Parse(reader.Slice);

        public static byte GetByte(this SDFReader reader) => byte.Parse(reader.Slice);
        public static ushort GetUInt16(this SDFReader reader) => ushort.Parse(reader.Slice);
        public static uint GetUInt32(this SDFReader reader) => uint.Parse(reader.Slice);
        public static ulong GetUInt64(this SDFReader reader) => ulong.Parse(reader.Slice);

        public static float GetSingle(this SDFReader reader) => float.Parse(reader.Slice);
        public static double GetDouble(this SDFReader reader) => double.Parse(reader.Slice);

        public static bool GetBoolean(this SDFReader reader) => bool.Parse(reader.Slice);

        public static string GetString(this SDFReader reader) => reader.Slice.ToString();
        #endregion

        #region TryGet
        public static bool TryGetSByte(this SDFReader reader, out sbyte result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && sbyte.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetInt16(this SDFReader reader, out short result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && short.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetInt32(this SDFReader reader, out int result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && int.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetInt64(this SDFReader reader, out long result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && long.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetByte(this SDFReader reader, out byte result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && byte.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetUInt16(this SDFReader reader, out ushort result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && ushort.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetUInt32(this SDFReader reader, out uint result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && uint.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetUInt64(this SDFReader reader, out ulong result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && ulong.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetSingle(this SDFReader reader, out float result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && float.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetDouble(this SDFReader reader, out double result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Number && double.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetBoolean(this SDFReader reader, out bool result)
        {
            result = default;
            if (reader.TokenType == SDFTokenType.Boolean && bool.TryParse(reader.Slice, out result))
                return true;
            return false;
        }

        public static bool TryGetBoolean(this SDFReader reader, out string? result)
        {
            result = null;
            if (reader.TokenType == SDFTokenType.String)
            {
                result = reader.Slice.IsEmpty ? string.Empty : reader.Slice.ToString();
                return true;
            }
            return false;
        }
        #endregion
    }
}
