using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EditorUI.Utility
{
    internal static class EnumHelper<T>
    {
        internal static T Parse(ReadOnlySpan<char> value, bool ignoreCase) => ValueDelegate(value, ignoreCase);

        internal static readonly TryParseDelegate ValueDelegate = (TryParseDelegate)Delegate.CreateDelegate(
            typeof(TryParseDelegate), typeof(Enum)
                .GetMethod(nameof(Enum.Parse), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(typeof(ReadOnlySpan<char>), typeof(bool)));
        internal delegate T TryParseDelegate(ReadOnlySpan<char> value, bool ignoreCase);
    }
}
