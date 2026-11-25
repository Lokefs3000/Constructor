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
    internal sealed class Vector4ValueConverter : ValueConverter<Vector4>
    {
        public override Vector4 TryDeserialize(ReadOnlySpan<char> source)
        {
            Vector4 value = default;

            var tokenizer = source.Tokenize(' ');

            tokenizer.MoveNext();
            value.X = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            tokenizer.MoveNext();
            value.Y = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            tokenizer.MoveNext();
            value.Z = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            tokenizer.MoveNext();
            value.W = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

            return value;
        }

        public override string TrySerialize(Vector4 value) => $"{value.X.ToString(CultureInfo.InvariantCulture)} {value.Y.ToString(CultureInfo.InvariantCulture)} {value.Z.ToString(CultureInfo.InvariantCulture)} {value.W.ToString(CultureInfo.InvariantCulture)}";
    }
}
