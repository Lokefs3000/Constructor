using Primary.Serialization.Structural;

namespace Primary.Scenes.Types
{
    internal sealed class SByteDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetIntegral(out long value))
                return (sbyte)value;
            return default(sbyte);
        }
    }

    internal sealed class ByteDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetUIntegral(out ulong value))
                return (byte)value;
            return default(byte);
        }
    }

    internal sealed class ShortDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetIntegral(out long value))
                return (short)value;
            return default(short);
        }
    }

    internal sealed class UShortDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetUIntegral(out ulong value))
                return (ushort)value;
            return default(ushort);
        }
    }

    internal sealed class IntDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetIntegral(out long value))
                return (int)value;
            return default(int);
        }
    }

    internal sealed class UIntDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetUIntegral(out ulong value))
                return (uint)value;
            return default(uint);
        }
    }

    internal sealed class LongDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetIntegral(out long value))
                return value;
            return default(long);
        }
    }

    internal sealed class ULongDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetUIntegral(out ulong value))
                return value;
            return default(ulong);
        }
    }

    internal sealed class SingleDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetNumber(out double value))
                return (float)value;
            return default(float);
        }
    }

    internal sealed class DoubleDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property && property.TryGetNumber(out double value))
                return value;
            return default(double);
        }
    }
}
