using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Diagnostics;

namespace Editor.Inspector
{
    public readonly record struct InspectorField
    {
        private readonly object _fieldOrProperty;
        private readonly bool _isPropertyType;
        private readonly int _sourceIndex;

        public InspectorField(FieldInfo fieldInfo, int sourceIndex = -1)
        {
            _fieldOrProperty = fieldInfo;
            _isPropertyType = false;
            _sourceIndex = sourceIndex;
        }

        public InspectorField(PropertyInfo propertyInfo, int sourceIndex = -1)
        {
            _fieldOrProperty = propertyInfo;
            _isPropertyType = true;
            _sourceIndex = sourceIndex;
        }

        public InspectorField(object fieldOrProperty, int sourceIndex = -1)
        {
            Guard.IsTrue(fieldOrProperty is FieldInfo or PropertyInfo);

            _fieldOrProperty = fieldOrProperty;
            _isPropertyType = fieldOrProperty is PropertyInfo;
            _sourceIndex = sourceIndex;
        }

        public Type GetCanonicalType()
        {
            Type type = FieldType;
            if (_sourceIndex != -1)
            {
                if (type.IsArray)
                    return type.GetElementType()!;
                if (type.IsGenericType)
                {
                    Type genericDefintion = type.GetGenericTypeDefinition();
                    if (genericDefintion == typeof(Span<>) || genericDefintion == typeof(ReadOnlySpan<>))
                        return type.GetGenericArguments()[0];
                }
            }

            return type;
        }

        public string Name => _isPropertyType ? Unsafe.As<PropertyInfo>(_fieldOrProperty).Name : Unsafe.As<FieldInfo>(_fieldOrProperty).Name;
        public Type FieldType => _isPropertyType ? Unsafe.As<PropertyInfo>(_fieldOrProperty).PropertyType : Unsafe.As<FieldInfo>(_fieldOrProperty).FieldType;
        
        public Type? DeclaringType => _isPropertyType ? Unsafe.As<PropertyInfo>(_fieldOrProperty).DeclaringType : Unsafe.As<FieldInfo>(_fieldOrProperty).DeclaringType;

        public bool IsDeclaredByClass => (_isPropertyType ? Unsafe.As<PropertyInfo>(_fieldOrProperty).DeclaringType : Unsafe.As<FieldInfo>(_fieldOrProperty).DeclaringType)?.IsClass ?? false;

        public object FieldOrProperty => _fieldOrProperty;
        public bool IsPropertyType => _isPropertyType;

        public int SourceIndex => _sourceIndex;

        public FieldInfo? Field => _fieldOrProperty as FieldInfo;
        public PropertyInfo? Property => _fieldOrProperty as PropertyInfo;
    }

    public delegate T GetInspectedByRefValue<T>(object fieldOrProperty, ref GenericRefValue target);
    public delegate void SetInspectedByRefValue<T>(object fieldOrProperty, ref GenericRefValue target, T value);

    public delegate T GetInspectedObjectValue<T>(object fieldOrProperty, object target);
    public delegate void SetInspectedObjectValue<T>(object fieldOrProperty, object target, T value);
}
