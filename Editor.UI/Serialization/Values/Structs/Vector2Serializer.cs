using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Datatypes;
using System.Globalization;
using System.Numerics;

namespace Editor.UI.Serialization.Values.Structs
{
    [ValueSerializerTarget(typeof(Vector2))]
    internal sealed class Vector2Serializer : IValueSerializer<Vector2>
    {
        public static string Serialize(Vector2 value) => $"{value.X.ToString(CultureInfo.InvariantCulture)} {value.Y.ToString(CultureInfo.InvariantCulture)}";
        public static bool Deserialize(string value, out Vector2 deserialized)
        {
            deserialized = Vector2.Zero;

            if (value.Count(' ') != 1)
                return false;

            ReadOnlySpanTokenizer<char> tokenizer = value.Tokenize(' ');

            if (!tokenizer.MoveNext())
                return false;
            if (!float.TryParse(tokenizer.Current, CultureInfo.InvariantCulture, out deserialized.X))
                return false;
            if (!tokenizer.MoveNext())
                return false;
            if (!float.TryParse(tokenizer.Current, CultureInfo.InvariantCulture, out deserialized.Y))
                return false;

            return true;
        }
    }
}
