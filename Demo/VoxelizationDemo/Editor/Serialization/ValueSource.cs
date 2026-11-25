using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace VoxelizationDemo.Editor.Serialization
{
    internal readonly record struct ValueSource
    {
        private readonly ValueSourceKind _kind;
        private readonly object _arg0;
        private readonly object? _arg1;
        private readonly object? _arg2;
        private readonly object? _arg3;

        private readonly Type _type;

        internal ValueSource(FieldInfo fieldInfo)
        {
            _kind = ValueSourceKind.Field;
            _arg0 = fieldInfo;

            _type = fieldInfo.FieldType;
        }

        internal ValueSource(PropertyInfo propertyInfo)
        {
            _kind = ValueSourceKind.Property;
            _arg0 = propertyInfo;

            _type = propertyInfo.PropertyType;
        }

        internal ValueSource(Delegate setter, Delegate setterProxy, Delegate getter, Delegate getterProxy, Type type)
        {
            _kind = ValueSourceKind.Delegate;
            _arg0 = setter;
            _arg1 = getter;
            _arg2 = setterProxy;
            _arg3 = getterProxy;

            _type = type;
        }

        public T? Get<T>(object boxed)
        {
            switch (_kind)
            {
                case ValueSourceKind.Delegate: return ((ValueSourceGetter<T>)_arg3!)((Delegate)_arg1!, boxed);
                case ValueSourceKind.Field: return (T?)((FieldInfo)_arg0).GetValue(boxed);
                case ValueSourceKind.Property: return (T?)((PropertyInfo)_arg0).GetValue(boxed);
                default: throw new NotSupportedException();
            }
        }

        public void Set<T>(object boxed, T? value)
        {
            switch (_kind)
            {
                case ValueSourceKind.Delegate: ((ValueSourceSetter<T>)_arg2!)((Delegate)_arg0!, boxed, value); break;
                case ValueSourceKind.Field: ((FieldInfo)_arg0).SetValue(boxed, value); break;
                case ValueSourceKind.Property: ((PropertyInfo)_arg0).SetValue(boxed, value); break;
                default: throw new NotSupportedException();
            }
        }

        public Type TargetType => _type;
    }

    public delegate void ValueSourceSetter<T>(Delegate source, object obj, T? value);
    public delegate T? ValueSourceGetter<T>(Delegate source, object obj);

    internal enum ValueSourceKind : byte
    {
        Delegate = 0,
        Field,
        Property
    }
}
