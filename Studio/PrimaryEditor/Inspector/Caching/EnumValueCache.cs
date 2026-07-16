using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using Primary.Collections;
using Primary.Editor;

namespace PrimaryEditor.Inspector.Caching
{
    public sealed class EnumValueCache
    {
        private readonly Dictionary<Type, EnumValueData> _enumValues;

        internal EnumValueCache()
        {
            _enumValues = new Dictionary<Type, EnumValueData>();
        }

        public EnumValueData GetEnumData(Type type)
        {
            Guard.IsTrue(type.IsEnum);

            ref EnumValueData valueData = ref CollectionsMarshal.GetValueRefOrAddDefault(_enumValues, type, out bool exists);
            if (!exists)
            {
                Type underlyingType = Enum.GetUnderlyingType(type);
                UnderlyingType underlyingTypeEnum = 0;

                if (underlyingType == typeof(sbyte))
                    underlyingTypeEnum = UnderlyingType.Int8;
                else if (underlyingType == typeof(short))
                    underlyingTypeEnum = UnderlyingType.Int16;
                else if (underlyingType == typeof(int))
                    underlyingTypeEnum = UnderlyingType.Int32;
                else if (underlyingType == typeof(long) || underlyingType == typeof(nint))
                    underlyingTypeEnum = UnderlyingType.Int64;
                else if (underlyingType == typeof(byte))
                    underlyingTypeEnum = UnderlyingType.UInt8;
                else if (underlyingType == typeof(ushort))
                    underlyingTypeEnum = UnderlyingType.UInt16;
                else if (underlyingType == typeof(uint))
                    underlyingTypeEnum = UnderlyingType.UInt32;
                else if (underlyingType == typeof(ulong))
                    underlyingTypeEnum = UnderlyingType.UInt64;
                else if (underlyingType == typeof(float))
                    underlyingTypeEnum = UnderlyingType.Single;
                else if (underlyingType == typeof(double))
                    underlyingTypeEnum = UnderlyingType.Double;
                else
                {
                    _enumValues.Remove(type);
                    throw new NotSupportedException(underlyingType.ToString());
                }

                using RentedList<string> nameList = [.. Enum.GetNames(type)];
                using RentedList<object> valueList = [.. Enum.GetValuesAsUnderlyingType(type)];

                for (int i = 0; i < nameList.Count; ++i)
                {
                    if (type.GetField(nameList[i])!.GetCustomAttribute<InspectorHiddenAttribute>() != null)
                    {
                        nameList.RemoveAt(i);
                        valueList.RemoveAt(i);

                        --i;
                    }
                }

                valueData = new EnumValueData(underlyingType, underlyingTypeEnum, [.. nameList], [.. valueList]);
            }

            return valueData;
        }

        public EnumValueData GetEnumData<T>() where T : Enum => GetEnumData(typeof(T));
    }

    public readonly record struct EnumValueData(Type UnderlyingType, UnderlyingType UnderlyingTypeEnum, string[] Names, object[] Values);

    public enum UnderlyingType : byte
    {
        Int8,
        Int16,
        Int32,
        Int64,

        UInt8,
        UInt16,
        UInt32,
        UInt64,

        Single,
        Double
    }
}
