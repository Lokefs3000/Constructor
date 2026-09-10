using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Components;
using Primary.Rendering.Assets;
using Primary.Scenes.Json.Converters;
using Primary.Scenes.Json.Helper;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Primary.Scenes.Json
{
    internal static class SceneValueConverter
    {
        internal static bool Deserialize<TComp, T>(ref Utf8JsonReader reader, ref SceneEntity entity, out T? value) where TComp : IComponent
        {
            Type type = typeof(T);

            if (type.IsEnum)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    SceneJsonSerializer.PrintWarning($"Invalid json at: {reader.Position}: Expected String for field but instead got {reader.TokenType}", entity, typeof(TComp));

                    value = default;
                    return false;
                }

                if (reader.ValueSpan.Length < 200)
                {
                    Span<char> temp = stackalloc char[reader.ValueSpan.Length];
                    for (int i = 0; i < reader.ValueSpan.Length; ++i)
                        temp[i] = (char)reader.ValueSpan[i];

                    return EnumGenericHelper<T>.TryParse(temp, false, out value);
                }
                else
                {
                    using RentedArray<char> temp = RentedArray<char>.Rent(reader.ValueSpan.Length);
                    for (int i = 0; i < reader.ValueSpan.Length; ++i)
                        temp[i] = (char)reader.ValueSpan[i];

                    return EnumGenericHelper<T>.TryParse(temp.Span, false, out value);
                }
            }
            else
            {
                value = default;

                if (type == typeof(Vector2)) return Vector2Converter.TryDeserialize(ref reader, ref Unsafe.As<T, Vector2>(ref value!));
                if (type == typeof(Vector3)) return Vector3Converter.TryDeserialize(ref reader, ref Unsafe.As<T, Vector3>(ref value!));
                if (type == typeof(Vector4)) return Vector4Converter.TryDeserialize(ref reader, ref Unsafe.As<T, Vector4>(ref value!));
                if (type == typeof(Quaternion)) return QuaternionConverter.TryDeserialize(ref reader, ref Unsafe.As<T, Quaternion>(ref value!));
                if (type == typeof(Color)) return ColorConverter.TryDeserialize(ref reader, ref Unsafe.As<T, Color>(ref value!));
                if (type.IsAssignableTo(typeof(IAssetDefinition))) return AssetGenericHelper<T>.TryDeserialize(ref reader, ref value, JsonSerializerOptions.Default);
            }

            EngLog.Scene.Error("Failed to find converter for type {t}", type);
            return false;
        }

        internal static bool Serialize(Utf8JsonWriter writer, object value, ref SceneEntity entity)
        {
            Type type = value.GetType();

            if (type.IsEnum)
            {
                string? str = Enum.ToObject(type, value).ToString();
                if (str == null)
                {
                    return false;
                }

                writer.WriteRawValue(str);
                return true;
            }
            else
            {
                if (type == typeof(Vector2)) return Vector2Converter.TrySerialize(writer, ref Unsafe.Unbox<Vector2>(value));
                if (type == typeof(Vector3)) return Vector3Converter.TrySerialize(writer, ref Unsafe.Unbox<Vector3>(value));
                if (type == typeof(Vector4)) return Vector4Converter.TrySerialize(writer, ref Unsafe.Unbox<Vector4>(value));
                if (type == typeof(Quaternion)) return QuaternionConverter.TrySerialize(writer, ref Unsafe.Unbox<Quaternion>(value));
                if (type == typeof(Color)) return ColorConverter.TrySerialize(writer, ref Unsafe.Unbox<Color>(value));
                if (type.IsAssignableTo(typeof(IAssetDefinition))) return AssetConverter.TrySerialize(writer, (IAssetDefinition)value, JsonSerializerOptions.Default);
            }

            EngLog.Scene.Error("Failed to find converter for type {t}", type);
            return false;
        }
    }
}
