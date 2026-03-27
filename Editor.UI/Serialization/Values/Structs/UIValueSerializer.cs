using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Datatypes;
using System.Globalization;

namespace Editor.UI.Serialization.Values.Structs
{
    [ValueSerializerTarget(typeof(UIValue))]
    internal sealed class UIValueSerializer : IValueSerializer<UIValue>
    {
        public static string Serialize(UIValue value) => $"{value.Absolute} {value.Relative.ToString(CultureInfo.InvariantCulture)}";
        public static bool Deserialize(string value, out UIValue deserialized)
        {
            deserialized = UIValue.Zero;

            if (value.Count(' ') != 1)
                return false;

            ReadOnlySpanTokenizer<char> tokenizer = value.Tokenize(' ');

            if (!tokenizer.MoveNext())
                return false;
            if (!float.TryParse(tokenizer.Current, CultureInfo.InvariantCulture, out deserialized.Relative))
                return false;
            if (!tokenizer.MoveNext())
                return false;
            if (!int.TryParse(tokenizer.Current, out deserialized.Absolute))
                return false;

            return true;
        }
    }
}
