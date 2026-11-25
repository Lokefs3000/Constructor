using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Datatypes;
using Primary.Mathematics;
using System.Globalization;
using System.Numerics;

namespace Editor.UI.Serialization.Values.Structs
{
    [ValueSerializerTarget(typeof(Int2))]
    internal sealed class Int2Serializer : IValueSerializer<Int2>
    {
        public static string Serialize(Int2 value) => $"{value.X.ToString(CultureInfo.InvariantCulture)} {value.Y.ToString(CultureInfo.InvariantCulture)}";
        public static bool Deserialize(string value, out Int2 deserialized)
        {
            deserialized = Int2.Zero;

            if (value.Count(' ') != 1)
                return false;

            ReadOnlySpanTokenizer<char> tokenizer = value.Tokenize(' ');

            if (!tokenizer.MoveNext())
                return false;
            if (!int.TryParse(tokenizer.Current, CultureInfo.InvariantCulture, out deserialized.X))
                return false;
            if (!tokenizer.MoveNext())
                return false;
            if (!int.TryParse(tokenizer.Current, CultureInfo.InvariantCulture, out deserialized.Y))
                return false;

            return true;
        }
    }
}
