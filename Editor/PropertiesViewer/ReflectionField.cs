using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Editor.PropertiesViewer
{
    internal interface IReflectionField
    {
        public void SetValue(TypedReference reference, object value);
        public object? GetValue(TypedReference reference);
    }

    internal class RFFieldImpl : IReflectionField
    {
        private FieldInfo _field;

        internal RFFieldImpl(FieldInfo field)
        {
            _field = field;
        }

        public object? GetValue(TypedReference reference)
        {
            return _field.GetValueDirect(reference);
        }

        public void SetValue(TypedReference reference, object value)
        {
            _field.SetValueDirect(reference, value);
        }
    }

    internal class RFPropertyImpl : IReflectionField
    {
        private PropertyInfo _property;

        internal RFPropertyImpl(PropertyInfo property)
        {
            _property = property;
        }

        public object? GetValue(TypedReference reference)
        {
            throw new NotImplementedException();
        }

        public void SetValue(TypedReference reference, object value)
        {
            throw new NotImplementedException();
        }
    }

    internal class ReflectionField
    {
        private readonly FieldType _type;
        private readonly object _value;

        internal ReflectionField(FieldInfo field)
        {
            _type = FieldType.Field;
            _value = field;
        }

        internal ReflectionField(PropertyInfo property)
        {
            _type = FieldType.Property;
            _value = property;
        }

        public object? GetValue(object? obj)
        {
            return _type == FieldType.Property ? Unsafe.As<PropertyInfo>(_value).GetValue(obj) : Unsafe.As<FieldInfo>(_value).GetValue(obj);
        }

        public void SetValue(object? obj, object? value)
        {
            if (_type == FieldType.Property)
                Unsafe.As<PropertyInfo>(_value).SetValue(obj, value);
            else
                Unsafe.As<FieldInfo>(_value).SetValue(obj, value);
        }

        public string Name
        {
            get => _type == FieldType.Property ? Unsafe.As<PropertyInfo>(_value).Name : Unsafe.As<FieldInfo>(_value).Name;
        }

        public Type Type
        {
            get => _type == FieldType.Property ? Unsafe.As<PropertyInfo>(_value).PropertyType : Unsafe.As<FieldInfo>(_value).FieldType;
        }

        public static implicit operator ReflectionField(FieldInfo fieldInfo) => new ReflectionField(fieldInfo);
        public static implicit operator ReflectionField(PropertyInfo propertyInfo) => new ReflectionField(propertyInfo);

        public object Value => _value;

        private enum FieldType : byte
        {
            Field = 0,
            Property
        }
    }
}
