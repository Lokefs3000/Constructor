using Primary.Common;
using Primary.Serialization.Structural;
using System.Numerics;

namespace Primary.Scenes.Types
{
    internal sealed class Vector2Deserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFArray array)
                return new Vector2((float)((SDFProperty)array[0]).GetNumber(), (float)((SDFProperty)array[1]).GetNumber());
            return Vector2.Zero;
        }
    }

    internal sealed class Vector3Deserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFArray array)
                return new Vector3((float)((SDFProperty)array[0]).GetNumber(), (float)((SDFProperty)array[1]).GetNumber(), (float)((SDFProperty)array[2]).GetNumber());
            return Vector3.Zero;
        }
    }

    internal sealed class Vector4Deserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFArray array)
                return new Vector4((float)((SDFProperty)array[0]).GetNumber(), (float)((SDFProperty)array[1]).GetNumber(), (float)((SDFProperty)array[2]).GetNumber(), (float)((SDFProperty)array[3]).GetNumber());
            return Vector4.Zero;
        }
    }

    internal sealed class QuaternionDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFArray array)
                return new Quaternion((float)((SDFProperty)array[0]).GetNumber(), (float)((SDFProperty)array[1]).GetNumber(), (float)((SDFProperty)array[2]).GetNumber(), (float)((SDFProperty)array[3]).GetNumber());
            return Quaternion.Identity;
        }
    }

    internal sealed class EnumDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property)
                return Enum.Parse(type, property.RawValueString!);
            return Enum.ToObject(type, 0);
        }
    }

    internal sealed class StringDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property)
                return property.RawValueString;
            return null;
        }
    }

    internal sealed class ColorDeserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFArray array)
                return new Color((float)((SDFProperty)array[0]).GetNumber(), (float)((SDFProperty)array[1]).GetNumber(), (float)((SDFProperty)array[2]).GetNumber(), (float)((SDFProperty)array[3]).GetNumber());
            return Color.Black;
        }
    }

    internal sealed class Color32Deserializer : ISceneTypeDeserializer
    {
        public object? Deserialize(ref SDFBase reader, Type type)
        {
            if (reader is SDFProperty property)
                return Color32.FromHex(property.GetString());
            return Color.Black;
        }
    }
}
