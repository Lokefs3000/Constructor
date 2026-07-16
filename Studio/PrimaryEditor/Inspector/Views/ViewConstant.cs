using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views.IO;

namespace PrimaryEditor.Inspector.Views
{
    public abstract record class ViewConstant(ViewSource Source)
    {
        public abstract bool Matches(IInspectorValue inspectorValue);
        public abstract bool Apply(IInspectorValue inspectorValue);

        public sealed record class ValueType<T>(ViewSource Source, T Value) : ViewConstant(Source) where T : unmanaged
        {
            internal ValueType(ViewSource source, PresetValueJson sourceData) : this(ResolvePresetValue(source, sourceData))
            {
            }

            public override bool Matches(IInspectorValue inspectorValue)
            {
                Type valueTargetType = inspectorValue.TargetType;
                if (valueTargetType != typeof(T))
                    return false;

                ref T valueToCompare = ref Unsafe.As<OpaqueRef, T>(ref inspectorValue.GetValueType());
                if (Value is IEquatable<T> equatable)
                    return equatable.Equals(valueToCompare);
                else
                    return Value.Equals(valueToCompare);
            }

            public override bool Apply(IInspectorValue inspectorValue)
            {
                Type valueTargetType = inspectorValue.TargetType;
                if (valueTargetType != typeof(T))
                    return false;

                inspectorValue.SetValueType(Value);
                return true;
            }

            private static ValueType<T> ResolvePresetValue(ViewSource source, PresetValueJson sourceData)
            {
                ArgumentNullException.ThrowIfNull(sourceData.Value);

                Type sourceType = typeof(T);
                Type primitiveType = sourceType.IsEnum ? typeof(string) : sourceType;

                if (sourceData.Value is not JsonElement element)
                    throw new InvalidDataException("Expected primitive type");

                if (primitiveType == typeof(sbyte))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<sbyte, T>(element.GetSByte()));
                }
                else if (primitiveType == typeof(byte))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<byte, T>(element.GetByte()));
                }
                else if (primitiveType == typeof(short))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<short, T>(element.GetInt16()));
                }
                else if (primitiveType == typeof(ushort))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<ushort, T>(element.GetUInt16()));
                }
                else if (primitiveType == typeof(int))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<int, T>(element.GetInt32()));
                }
                else if (primitiveType == typeof(uint))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<uint, T>(element.GetUInt32()));
                }
                else if (primitiveType == typeof(long))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<long, T>(element.GetInt64()));
                }
                else if (primitiveType == typeof(ulong))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<ulong, T>(element.GetUInt64()));
                }
                else if (primitiveType == typeof(bool))
                {
                    return new ValueType<T>(source, Unsafe.BitCast<bool, T>(element.GetBoolean()));
                }
                else if (primitiveType == typeof(string))
                {
                    if (!sourceType.IsEnum)
                        throw new InvalidDataException("Invalid primitive type");
                    return new ValueType<T>(source, (T)Enum.Parse(sourceType, element.GetString() ?? string.Empty));
                }
                else
                {
                    throw new NotSupportedException(primitiveType.FullName);
                }
            }
        }

        public sealed record class Object<T>(ViewSource Source, T? Value) : ViewConstant(Source) where T : class
        {
            internal Object(ViewSource source, PresetValueJson sourceData) : this(ResolvePresetValue(source, sourceData))
            {
            }

            public override bool Matches(IInspectorValue inspectorValue)
            {
                Type valueTargetType = inspectorValue.TargetType;
                if (valueTargetType != typeof(T))
                    return false;

                object? valueToCompare = inspectorValue.GetObjectValue();
                if (Value == null)
                    return valueToCompare == null;

                if (Value is IEquatable<T> equatable)
                    return equatable.Equals(valueToCompare);
                else
                    return Value.Equals(valueToCompare);
            }

            public override bool Apply(IInspectorValue inspectorValue)
            {
                Type valueTargetType = inspectorValue.TargetType;
                if (valueTargetType != typeof(T))
                    return false;

                inspectorValue.SetObject(Value);
                return true;
            }

            private static Object<T> ResolvePresetValue(ViewSource source, PresetValueJson sourceData)
            {
                ArgumentNullException.ThrowIfNull(sourceData.Value);

                Type sourceType = typeof(T);

                if (sourceData.Value is not JsonElement element)
                    throw new InvalidDataException("Expected object type");

                if (sourceType == typeof(string))
                {
                    return new Object<T>(source, (T)Convert.ChangeType(element.GetString() ?? string.Empty, typeof(T)));
                }
                else
                {
                    throw new NotSupportedException(sourceType.FullName);
                }
            }
        }
    }
}
