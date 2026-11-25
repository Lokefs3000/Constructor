using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Serialization.Value
{
    [ValueConverter]
    internal sealed class StringValueConverter : ValueConverter<string>
    {
        public override string? TryDeserialize(ReadOnlySpan<char> source) => source.ToString();
        public override string TrySerialize(string? value) => value ?? string.Empty;
    }
}
