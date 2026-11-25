using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Mathematics;

namespace EditorUI.Serialization.Value
{
    [ValueConverter]
    internal sealed class UIValue2ValueConverter : ValueConverter<UIValue2>
    {
        public override UIValue2 TryDeserialize(ReadOnlySpan<char> source)
        {
            UIValue2 value = default;

            var tokenizer = source.Tokenize(' ');

            // x

            tokenizer.MoveNext();
            value.X.Relative = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            tokenizer.MoveNext();
            value.X.Absolute = int.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            // y

            tokenizer.MoveNext();
            value.Y.Relative = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            tokenizer.MoveNext();
            value.Y.Absolute = int.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            return value;
        }

        public override string TrySerialize(UIValue2 value) => $"{value.X.Relative.ToString(CultureInfo.InvariantCulture)} {value.X.Absolute} {value.Y.Relative.ToString(CultureInfo.InvariantCulture)} {value.Y.Absolute}";
    }
}
