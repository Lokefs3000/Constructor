using Primary.Scenes.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Primary.Scenes.Json.Helper
{
    internal static class AssetGenericHelper<T>
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(AssetConverter))]
        internal static bool TryDeserialize(ref Utf8JsonReader reader, ref T? value) => ValueDelegate(ref reader, ref value);

        internal static readonly TryDeserializeDelegate ValueDelegate = (TryDeserializeDelegate)Delegate.CreateDelegate(
            typeof(TryDeserializeDelegate), typeof(AssetConverter)
                .GetMethod(nameof(AssetConverter.TryDeserialize), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(typeof(T)));
        internal delegate bool TryDeserializeDelegate(ref Utf8JsonReader reader, ref T? result);
    }
}
