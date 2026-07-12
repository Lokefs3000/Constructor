using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Mathematics;

namespace EditorUI.Serialization.Value
{
    [ValueConverter]
    internal sealed class Vector2ValueConverter : ValueConverter<Vector2>
    {
        public override Vector2 TryDeserialize(ReadOnlySpan<char> source)
        {
            Vector2 value = default;

            var tokenizer = source.Tokenize(' ');

            tokenizer.MoveNext();
            value.X = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            tokenizer.MoveNext();
            value.Y = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            return value;
        }

        public override string TrySerialize(Vector2 value) => $"{value.X.ToString(CultureInfo.InvariantCulture)} {value.Y.ToString(CultureInfo.InvariantCulture)}";
    }
}
