using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Editor.UI.Serialization.Helpers
{
    internal static class EnumGenericHelper<T>
    {
        internal static bool TryParse(ReadOnlySpan<char> value, bool ignoreCase, out T result) => ValueDelegate(value, ignoreCase, out result);

        internal static readonly TryParseDelegate ValueDelegate = (TryParseDelegate)Delegate.CreateDelegate(
            typeof(TryParseDelegate), typeof(Enum)
                .GetMethod(nameof(Enum.TryParse), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(typeof(ReadOnlySpan<char>), typeof(bool), typeof(T)));
        internal delegate bool TryParseDelegate(ReadOnlySpan<char> value, bool ignoreCase, out T result);
    }
}
