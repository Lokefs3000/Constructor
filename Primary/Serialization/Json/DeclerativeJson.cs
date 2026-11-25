using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Primary.Serialization.Json
{
    public ref struct DeclerativeJson
    {
        private Utf8JsonReader _reader;
        private bool _dontThrow;

        public DeclerativeJson(ref Utf8JsonReader reader, bool dontThrow = false)
        {
            _reader = reader;
            _dontThrow = dontThrow;
        }

        public bool StartObject(bool allowNull = false, bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || (_reader.TokenType != JsonTokenType.StartObject && !(allowNull && _reader.TokenType == JsonTokenType.Null)))
            {
                if (!_dontThrow)
                    ThrowException($"Expected StartObject token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        public bool EndObject(bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || _reader.TokenType != JsonTokenType.EndObject)
            {
                if (!_dontThrow)
                    ThrowException($"Expected EndObject token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        public bool StartArray(bool allowNull = false, bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || (_reader.TokenType != JsonTokenType.StartArray && !(allowNull && _reader.TokenType == JsonTokenType.Null)))
            {
                if (!_dontThrow)
                    ThrowException($"Expected StartArray token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        public bool EndArray(bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || _reader.TokenType != JsonTokenType.EndArray)
            {
                if (!_dontThrow)
                    ThrowException($"Expected EndArray token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        public bool PropertyName(bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || _reader.TokenType != JsonTokenType.PropertyName)
            {
                if (!_dontThrow)
                    ThrowException($"Expected PropertyName token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        public bool String(bool allowNull = false, bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || (_reader.TokenType != JsonTokenType.String && !(allowNull && _reader.TokenType == JsonTokenType.Null)))
            {
                if (!_dontThrow)
                    ThrowException($"Expected String token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        public bool Number(bool allowNull = false, bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || (_reader.TokenType != JsonTokenType.Number && !(allowNull && _reader.TokenType == JsonTokenType.Null)))
            {
                if (!_dontThrow)
                    ThrowException($"Expected String token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        public bool Boolean(bool allowNull = false, bool dontRead = false)
        {
            if ((dontRead || !_reader.Read()) || ((_reader.TokenType != JsonTokenType.True && _reader.TokenType != JsonTokenType.False) && !(allowNull && _reader.TokenType == JsonTokenType.Null)))
            {
                if (!_dontThrow)
                    ThrowException($"Expected String token but instead got {_reader.TokenType}");
                return false;
            }

            return true;
        }

        [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowException(string message) => throw new JsonDeclerationException(message);
    }

    public class JsonDeclerationException : Exception
    {
        public JsonDeclerationException(string? message)
        {
        }
    }
}
