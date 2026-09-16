using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace VoxelizationDemo.Editor.Serialization
{
    internal readonly record struct SerializationProperty
    {
        private readonly object _baseObject;
        private readonly bool _isProperty;

        public SerializationProperty(FieldInfo fieldInfo)
        {
            _baseObject = fieldInfo;
            _isProperty = false;
        }

        public SerializationProperty(PropertyInfo propertyInfo)
        {
            _baseObject = propertyInfo;
            _isProperty = true;
        }

        public readonly object? GetValue(object? obj)
        {
            if (_isProperty)
                return Unsafe.As<PropertyInfo>(_baseObject).GetValue(obj);
            else
                return Unsafe.As<FieldInfo>(_baseObject).GetValue(obj);
        }

        public readonly void SetValue(object? obj, object? value)
        {
            if (_isProperty)
                Unsafe.As<PropertyInfo>(_baseObject).SetValue(obj, value);
            else
                Unsafe.As<FieldInfo>(_baseObject).SetValue(obj, value);
        }

        public FieldInfo Field => (FieldInfo)_baseObject;
        public PropertyInfo Property => (PropertyInfo)_baseObject;

        public bool IsProperty => _isProperty;

        public static implicit operator SerializationProperty(FieldInfo fieldInfo) => new SerializationProperty(fieldInfo);
        public static implicit operator SerializationProperty(PropertyInfo propertyInfo) => new SerializationProperty(propertyInfo);
    }
}
