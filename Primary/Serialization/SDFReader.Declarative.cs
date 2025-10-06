using System.Runtime.Serialization;

namespace Primary.Serialization
{
    public static class SDFReaderDeclarative
    {
        public static void BeginObject(this SDFReader reader, string? objectName = null, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.ObjectBegin || (objectName != null && reader.GetString() == objectName))
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"{objectName ?? string.Empty}{{\"");
            }
        }

        public static void EndObject(this SDFReader reader, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.ObjectEnd)
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"}}\"");
            }
        }

        public static void BeginArray(this SDFReader reader, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.ArrayBegin)
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"[\"");
            }
        }

        public static void EndArray(this SDFReader reader, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.ArrayEnd)
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"]\"");
            }
        }

        public static void Property(this SDFReader reader, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.Property)
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"PROPERTY_NAME = \"");
            }
        }

        public static void Number(this SDFReader reader, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.Number)
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"NUMBER\"");
            }
        }

        public static void Boolean(this SDFReader reader, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.Boolean)
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"BOOLEAN\"");
            }
        }

        public static void String(this SDFReader reader, bool dontRead = false)
        {
            if (!(!dontRead && reader.Read()) || reader.TokenType != SDFTokenType.Boolean)
            {
                throw new SDFDeclarativeException($"[{reader.Line}:{reader.Index}]: Invalid decleration found, expected: \"STRING\"");
            }
        }
    }

    public class SDFDeclarativeException : Exception
    {
        public SDFDeclarativeException() { }
        public SDFDeclarativeException(string? message) : base(message) { }
        public SDFDeclarativeException(string? message, Exception? innerException) : base(message, innerException) { }
        protected SDFDeclarativeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
