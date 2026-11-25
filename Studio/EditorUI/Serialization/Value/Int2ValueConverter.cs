using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Mathematics;
using Primary.Mathematics;

namespace EditorUI.Serialization.Value
{
    [ValueConverter]
    internal sealed class Int2ValueConverter : ValueConverter<Int2>
    {
        public override Int2 TryDeserialize(ReadOnlySpan<char> source)
        {
            Int2 value = default;

            var tokenizer = source.Tokenize(' ');

            tokenizer.MoveNext();
            value.X = int.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            tokenizer.MoveNext();
            value.Y = int.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            return value;
        }

        public override string TrySerialize(Int2 value) => $"{value.X.ToString(CultureInfo.InvariantCulture)} {value.Y.ToString(CultureInfo.InvariantCulture)}";
    }
}
