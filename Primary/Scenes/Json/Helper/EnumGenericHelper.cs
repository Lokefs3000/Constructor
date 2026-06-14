using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Primary.Scenes.Json.Helper
{
    internal static class EnumGenericHelper<T>
    {
        internal static bool TryParse(ReadOnlySpan<char> value, bool ignoreCase, out T result) => ValueDelegate(value, ignoreCase, out result);

        internal static readonly TryParseDelegate ValueDelegate = (TryParseDelegate)Delegate.CreateDelegate(
            typeof(TryParseDelegate), typeof(Enum)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .SingleOrDefault(static (x) =>
                {
                    if (x.Name != "TryParse" || !x.ContainsGenericParameters)
                        return false;

                    ParameterInfo[] @params = x.GetParameters();
                    if (@params.Length != 3 || @params[0].ParameterType != typeof(ReadOnlySpan<char>))
                        return false;

                    return true;
                })
                .MakeGenericMethod(typeof(T)));
        internal delegate bool TryParseDelegate(ReadOnlySpan<char> value, bool ignoreCase, out T result);
    }
}
