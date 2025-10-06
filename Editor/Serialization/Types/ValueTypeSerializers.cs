using Primary.Common;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Editor.Serialization.Types
{
    internal sealed class Vector2Serializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Vector2 val = (Vector2)obj!;
            sb.Append('[');
            sb.Append(val.X.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.Y.ToString(CultureInfo.InvariantCulture));
            sb.Append(']');
        }
    }

    internal sealed class Vector3Serializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Vector3 val = (Vector3)obj!;
            sb.Append('[');
            sb.Append(val.X.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.Y.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.Z.ToString(CultureInfo.InvariantCulture));
            sb.Append(']');
        }
    }

    internal sealed class Vector4Serializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Vector4 val = (Vector4)obj!;
            sb.Append('[');
            sb.Append(val.X.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.Y.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.Z.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.W.ToString(CultureInfo.InvariantCulture));
            sb.Append(']');
        }
    }

    internal sealed class QuaternionSerializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Quaternion val = (Quaternion)obj!;
            sb.Append('[');
            sb.Append(val.X.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.Y.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.Z.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.W.ToString(CultureInfo.InvariantCulture));
            sb.Append(']');
        }
    }

    internal sealed class StringSerializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            if (obj == null)
            {
                sb.Append("null");
                return;
            }

            string val = (string)obj;
            val = val.Replace("\"", "\\\"", StringComparison.InvariantCulture);

            sb.Append('"');
            sb.Append(val);
            sb.Append('"');
        }
    }

    internal sealed class EnumSerializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Enum @enum = (Enum)obj!;

            sb.Append('"');
            sb.Append(@enum.ToString());
            sb.Append('"');
        }
    }

    internal sealed class ColorSerializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Color val = (Color)obj!;
            sb.Append('[');
            sb.Append(val.R.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.G.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.B.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(val.A.ToString(CultureInfo.InvariantCulture));
            sb.Append(']');
        }
    }

    internal sealed class Color32Serializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Color32 val = (Color32)obj!;
            sb.Append(val.RGBA.ToString("x8"));
        }
    }
}
