using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Datatypes;
using System.Globalization;

namespace Editor.UI.Serialization.Values.Structs
{
    internal sealed class UIValue2Serializer : ValueSerializer<UIValue2>
    {
        public override string Serialize(UIValue2 value) => $"{value.X.Absolute},{value.X.Relative.ToString(CultureInfo.InvariantCulture)},{value.Y.Absolute},{value.Y.Relative.ToString(CultureInfo.InvariantCulture)}";
        public override bool Deserialize(string value, out UIValue2 deserialized)
        {
            deserialized = UIValue2.Zero;

            if (value.Count(',') != 3)
                return false;

            ReadOnlySpanTokenizer<char> tokenizer = value.Tokenize(',');

            if (!tokenizer.MoveNext())
                return false;

            {
                if (!int.TryParse(tokenizer.Current, out deserialized.X.Absolute))
                    return false;
                if (!tokenizer.MoveNext())
                    return false;
                if (!float.TryParse(tokenizer.Current, CultureInfo.InvariantCulture, out deserialized.X.Relative))
                    return false;
            }

            if (!tokenizer.MoveNext())
                return false;

            {
                if (!int.TryParse(tokenizer.Current, out deserialized.Y.Absolute))
                    return false;
                if (!tokenizer.MoveNext())
                    return false;
                if (!float.TryParse(tokenizer.Current, CultureInfo.InvariantCulture, out deserialized.Y.Relative))
                    return false;
            }

            return true;
        }
    }
}
