using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Serialization.Value
{
    public abstract class ValueConverter<T> : ValueConverter
    {
        public abstract T? TryDeserialize(ReadOnlySpan<char> source);
        public abstract string TrySerialize(T? value);

        // generic interface

        public override object? TryDeserializeBoxed(ReadOnlySpan<char> source) => TryDeserialize(source);
        public override string TrySerializeBoxed(object? boxed) => TrySerialize((T?)boxed);
    }

    public abstract class ValueConverter
    {
        public abstract object? TryDeserializeBoxed(ReadOnlySpan<char> source);
        public abstract string TrySerializeBoxed(object? boxed);
    }
}
