using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Datatypes;
using Primary.Common;
using System.Globalization;

namespace Editor.UI.Serialization.Values.Structs
{
    [ValueSerializerTarget(typeof(Color))]
    internal sealed class ColorSerializer : IValueSerializer<Color>
    {
        public static string Serialize(Color value) => $"{value.R.ToString(CultureInfo.InvariantCulture)} {value.G.ToString(CultureInfo.InvariantCulture)} {value.B.ToString(CultureInfo.InvariantCulture)} {value.A.ToString(CultureInfo.InvariantCulture)}";

        public static bool Deserialize(string value, out Color deserialized)
        {
            deserialized = Color.TransparentBlack;

            if (value.StartsWith('#'))
            {
                deserialized = Color.FromHex(value.Substring(1));
                return true;
            }

            if (value.Count(' ') != 3)
                return false;

            ReadOnlySpanTokenizer<char> tokenizer = value.Tokenize(' ');

            if (!tokenizer.MoveNext())
                return false;

            if (!float.TryParse(tokenizer.Current, out deserialized.R) || !tokenizer.MoveNext())
                return false;
            if (!float.TryParse(tokenizer.Current, out deserialized.G) || !tokenizer.MoveNext())
                return false;
            if (!float.TryParse(tokenizer.Current, out deserialized.B) || !tokenizer.MoveNext())
                return false;
            if (!float.TryParse(tokenizer.Current, out deserialized.A))
                return false;

            if (deserialized.R > 1.0f)
                deserialized = Color.Normalize(deserialized);

            return true;
        }
    }
}
