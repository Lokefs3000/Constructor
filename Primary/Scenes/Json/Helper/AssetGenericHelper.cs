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
        internal static bool TryDeserialize(ref Utf8JsonReader reader, ref T? value, JsonSerializerOptions options) => ValueDelegate(ref reader, ref value, options);

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(AssetConverter))]
        internal static bool TrySerialize(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => Value2Delegate(writer, value, options);

        internal static readonly TryDeserializeDelegate ValueDelegate = (TryDeserializeDelegate)Delegate.CreateDelegate(
            typeof(TryDeserializeDelegate), typeof(AssetConverter)
                .GetMethod(nameof(AssetConverter.TryDeserialize), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(typeof(T)));
        internal delegate bool TryDeserializeDelegate(ref Utf8JsonReader reader, ref T? result, JsonSerializerOptions options);

        internal static readonly TrySerializeDelegate Value2Delegate = (TrySerializeDelegate)Delegate.CreateDelegate(
            typeof(TrySerializeDelegate), typeof(AssetConverter)
                .GetMethod(nameof(AssetConverter.TrySerialize), BindingFlags.Static | BindingFlags.Public)!
                .MakeGenericMethod(typeof(T)));
        internal delegate bool TrySerializeDelegate(Utf8JsonWriter writer, object value, JsonSerializerOptions options);
    }
}
