using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Primary.Collections;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views.IO;
using SharpGen.Runtime;

namespace PrimaryEditor.Inspector.Views
{
    public abstract record class ViewCondition(ViewSource Source)
    {
        public abstract bool HasPassedCondition(IInspectorValue inspectorValue);

        public sealed record class ValueType<T>(ViewSource Source, object? Value) : ViewCondition(Source) where T : unmanaged
        {
            internal ValueType(ViewSource conditionSource, ConditionJson sourceData) : this(ResolveConditionValue(conditionSource, sourceData))
            {
            }

            public override bool HasPassedCondition(IInspectorValue inspectorValue)
            {
                if (Value == null)
                    return true;

                Type valueTargetType = inspectorValue.TargetType;
                if (valueTargetType != typeof(T))
                    return false;

                ref T valueToCompare = ref Unsafe.As<OpaqueRef, T>(ref inspectorValue.GetValueType());
                if (Value is FrozenSet<T> set)
                {
                    return set.Contains(valueToCompare);
                }
                else
                {
                    if (Value is IEquatable<T> equatable)
                        return equatable.Equals(valueToCompare);
                    else
                        return Value.Equals(valueToCompare);
                }
            }

            private static ValueType<T> ResolveConditionValue(ViewSource conditionSource, ConditionJson sourceData)
            {
                ArgumentNullException.ThrowIfNull(sourceData.Values);

                Type sourceType = typeof(T);
                Type primitiveType = sourceType.IsEnum ? sourceType.GetEnumUnderlyingType() : sourceType;

                int primitiveByteSize = 0;
                bool isUnsigned = false;

                if (primitiveType == typeof(sbyte))
                    (primitiveByteSize, isUnsigned) = (1, false);
                else if (primitiveType == typeof(byte))
                    (primitiveByteSize, isUnsigned) = (1, true);
                else if (primitiveType == typeof(short))
                    (primitiveByteSize, isUnsigned) = (2, false);
                else if (primitiveType == typeof(ushort))
                    (primitiveByteSize, isUnsigned) = (2, true);
                else if (primitiveType == typeof(int))
                    (primitiveByteSize, isUnsigned) = (4, false);
                else if (primitiveType == typeof(uint))
                    (primitiveByteSize, isUnsigned) = (4, true);
                else if (primitiveType == typeof(long))
                    (primitiveByteSize, isUnsigned) = (8, false);
                else if (primitiveType == typeof(ulong))
                    (primitiveByteSize, isUnsigned) = (8, true);
                else if (primitiveType == typeof(bool))
                    (primitiveByteSize, isUnsigned) = (-1, true);
                else
                    throw new NotSupportedException(primitiveType.FullName);

                using RentedList<T> values = new RentedList<T>(sourceData.Values.Length);
                for (int i = 0; i < sourceData.Values.Length; ++i)
                {
                    object? value = sourceData.Values[i];
                    ArgumentNullException.ThrowIfNull(value);

                    JsonElement element = (JsonElement)value;
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        if (!sourceType.IsEnum)
                            throw new InvalidDataException("Invalid primitive type");

                        // It's suboptimal but the GC can handle it
                        values.Add((T)Enum.Parse(sourceType, element.GetString() ?? string.Empty));
                    }
                    else
                    {
                        switch (primitiveByteSize)
                        {
                            case 1: values.Add(isUnsigned ? Unsafe.BitCast<byte, T>(element.GetByte()) : Unsafe.BitCast<sbyte, T>(element.GetSByte())); break;
                            case 2: values.Add(isUnsigned ? Unsafe.BitCast<ushort, T>(element.GetUInt16()) : Unsafe.BitCast<short, T>(element.GetInt16())); break;
                            case 4: values.Add(isUnsigned ? Unsafe.BitCast<uint, T>(element.GetUInt32()) : Unsafe.BitCast<int, T>(element.GetInt32())); break;
                            case 8: values.Add(isUnsigned ? Unsafe.BitCast<ulong, T>(element.GetUInt64()) : Unsafe.BitCast<long, T>(element.GetInt64())); break;

                            case -1: values.Add(Unsafe.BitCast<bool, T>(element.GetBoolean())); break;
                        }
                    }
                }

                if (values.IsEmpty)
                {
                    return new ValueType<T>(conditionSource, (object?)null);
                }
                else if (values.Count == 1)
                {
                    return new ValueType<T>(conditionSource, values[0]);
                }
                else
                {
                    return new ValueType<T>(conditionSource, (FrozenSet<T>)[.. values]);
                }
            }
        }

        public sealed record class Object<T>(ViewSource Source, object? Value) : ViewCondition(Source) where T : class
        {
            internal Object(ViewSource conditionSource, ConditionJson sourceData) : this(ResolveConditionValue(conditionSource, sourceData))
            {
            }

            public override bool HasPassedCondition(IInspectorValue inspectorValue)
            {
                Type valueTargetType = inspectorValue.TargetType;
                if (valueTargetType != typeof(T))
                    return false;

                object? valueToCompare = inspectorValue.GetObjectValue();
                if (Value == null)
                    return valueToCompare == null;

                if (Value is FrozenSet<T?> set)
                {
                    return set.Contains(valueToCompare);
                }
                else
                {
                    if (Value is IEquatable<T> equatable)
                        return equatable.Equals(valueToCompare);
                    else
                        return Value.Equals(valueToCompare);
                }
            }

            private static Object<T> ResolveConditionValue(ViewSource conditionSource, ConditionJson sourceData)
            {
                if (sourceData.Values == null || (sourceData.Values.Length == 1 && sourceData.Values[0] == null))
                    return new Object<T>(conditionSource, (object?)null);

                Type sourceType = conditionSource.Type;

                if (sourceType != typeof(string))
                    throw new NotSupportedException(sourceType.FullName);

                using RentedList<T?> values = new RentedList<T?>(sourceData.Values.Length);
                for (int i = 0; i < sourceData.Values.Length; ++i)
                {
                    object? value = sourceData.Values[i];
                    if (value != null)
                    {
                        JsonElement element = (JsonElement)value;
                        if (sourceType == typeof(string))
                        {
                            values.Add((T?)Convert.ChangeType(element.GetString(), sourceType));
                        }
                    }
                    else
                    {
                        values.Add(null);
                    }
                }

                if (values.IsEmpty)
                {
                    return new Object<T>(conditionSource, (object?)null);
                }
                else if (values.Count == 1)
                {
                    return new Object<T>(conditionSource, values[0]);
                }
                else
                {
                    return new Object<T>(conditionSource, (FrozenSet<T?>)[.. values]);
                }
            }
        }
    }
}
