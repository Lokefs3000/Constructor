using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Mathematics;

namespace EditorUI.Serialization.Value
{
    [ValueConverter]
    internal sealed class UIValueValueConverter : ValueConverter<UIValue>
    {
        public override UIValue TryDeserialize(ReadOnlySpan<char> source)
        {
            UIValue value = default;

            var tokenizer = source.Tokenize(' ');

            tokenizer.MoveNext();
            value.Relative = float.Parse(tokenizer.Current);

            tokenizer.MoveNext();
            value.Absolute = int.Parse(tokenizer.Current);

            return value;
        }

        public override string TrySerialize(UIValue value) => $"{value.Relative.ToString(CultureInfo.InvariantCulture)} {value.Absolute}";
    }
}
