using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Primary.Assets.Types;
using Primary.Common;
using VoxelizationDemo.Editor.Serialization.Converters;

namespace VoxelizationDemo.Editor.Serialization
{
    internal sealed class ConverterDatabase
    {
        private readonly ConcurrentDictionary<Type, ITypeConverter?> _entries;

        internal ConverterDatabase()
        {
            _entries = new ConcurrentDictionary<Type, ITypeConverter?>();
        }

        internal bool TryGetConverter(Type type, [NotNullWhen(true)] out ITypeConverter? typeConverter)
        {
            return (typeConverter = _entries.GetOrAdd(type, CreateEntry)) != null;
        }

        private ITypeConverter? CreateEntry(Type type)
        {
            Type? converterType = null;
            ITypeConverter? typeConverter = null;

            {
                Type? currentType = type;
                while (currentType != null && !s_predefinedConverters.TryGetValue(currentType, out converterType))
                {
                    currentType = currentType.BaseType;
                }

                if (converterType == null)
                {
                    AppLog.Editor.Warning("Failed to find converter for type '{t}'", type);
                    return null;
                }
                else if (currentType != null && currentType != type)
                {
                    if (TryGetConverter(currentType, out typeConverter))
                        return typeConverter;
                }
            }

            try
            {
                typeConverter = (ITypeConverter)Activator.CreateInstance(converterType)!;
            }
            catch (Exception ex)
            {
                AppLog.Editor.Error(ex, "Failed to activate type converter '{t}'", type);
                return null;
            }

            return typeConverter;
        }

        private static readonly FrozenDictionary<Type, Type> s_predefinedConverters = new Dictionary<Type, Type>
        {
            { typeof(sbyte), typeof(SByteTypeConverter) },
            { typeof(short), typeof(Int16TypeConverter) },
            { typeof(int), typeof(Int32TypeConverter) },
            { typeof(long), typeof(Int64TypeConverter) },

            { typeof(byte), typeof(ByteTypeConverter) },
            { typeof(ushort), typeof(UInt16TypeConverter) },
            { typeof(uint), typeof(UInt32TypeConverter) },
            { typeof(ulong), typeof(UInt64TypeConverter) },

            { typeof(bool), typeof(BooleanTypeConverter) },

            { typeof(float), typeof(SingleTypeConverter) },
            { typeof(double), typeof(DoubleTypeConverter) },

            { typeof(Vector2), typeof(Vector2TypeConverter) },
            { typeof(Vector3), typeof(Vector3TypeConverter) },
            { typeof(Vector4), typeof(Vector4TypeConverter) },
            { typeof(Quaternion), typeof(QuaternionTypeConverter) },
            { typeof(Color), typeof(ColorTypeConverter) },

            { typeof(string), typeof(StringTypeConverter) },

            { typeof(BaseAssetDefinition), typeof(AssetDefinitionTypeConverter) }
        }.ToFrozenDictionary();
    }
}
