using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using PrimaryEditor.Inspector.Reflection;

namespace PrimaryEditor.Inspector.Values
{
    public class InspectorValue<TObject, T> : IInspectorValue
    {
        protected readonly IInspectorObject? _parentObject;
        protected readonly InspectorValueSource _valueSource;
        protected readonly string _name;

        protected T? _value;
        protected bool _hasModifiedValue;

        protected int _uniqueHash;

        internal InspectorValue(IInspectorObject? parentObject, InspectorValueSource valueSource, string name)
        {
            _parentObject = parentObject;
            _valueSource = valueSource;
            _name = name;

            _value = default;
            _hasModifiedValue = false;

            _uniqueHash = parentObject != null ? HashCode.Combine(parentObject.UniqueHash, valueSource.TargetName) : valueSource.TargetName.GetHashCode();
        }

        public void UpdateValueFromValueType(ref OpaqueRef valueType)
        {
            if (_hasModifiedValue)
                _valueSource.SetValueFromValueType<T, TObject>(ref Unsafe.As<OpaqueRef, TObject>(ref valueType), _value);
            _value = _valueSource.GetValueFromValueType<T, TObject>(ref Unsafe.As<OpaqueRef, TObject>(ref valueType));
        }

        public void UpdateValueFromObject(object obj)
        {
            if (_hasModifiedValue)
                _valueSource.SetValueFromObject<T, TObject>((TObject)obj, _value);
            _value = _valueSource.GetValueFromObject<T, TObject>((TObject)obj);
        }

        public ref OpaqueRef GetValueType() => ref Unsafe.As<T, OpaqueRef>(ref _value!);
        public object? GetObjectValue() => _value;

        public virtual bool Equals(IInspectorValue? other)
        {
            if (other is InspectorValue<TObject, T> value)
            {
                if (_value is IEquatable<T> equatable)
                    return equatable.Equals(value._value);
                else
                    return _value == null ? value._value == null : _value.Equals(value._value);
            }

            return false;
        }

        private void UpdateStoredValue(T? value)
        {
            _value = value;
            _hasModifiedValue = true;
        }

        public void SetValueType<T1>(T1 value) where T1 : unmanaged
        {
            if (!typeof(T).IsValueType)
                throw new InvalidOperationException();

            _value = Unsafe.As<T1, T>(ref value);
            _hasModifiedValue = true;

            if (_parentObject?.TargetType.IsValueType ?? false)
                _parentObject.UpdateSelf();
        }

        public void SetObject<T1>(T1? value) where T1 : class
        {
            if (!typeof(T).IsClass && !typeof(T).IsInterface)
                throw new InvalidOperationException();

            _value = Unsafe.As<T1?, T?>(ref value);
            _hasModifiedValue = true;
        }

        public void UpdateSelf()
        {
            _hasModifiedValue = true;

            if (_parentObject?.TargetType.IsValueType ?? false)
                _parentObject.UpdateSelf();
        }

        public Type TargetType => typeof(T);
        public IInspectorValue? OwningValue => _parentObject;

        public string Name => _name;
        public string TargetName => _valueSource.LocalName;
        public string FullTargetName => _valueSource.TargetName;

        public int UniqueHash => _uniqueHash;

        public T? Value { get => _value; set => UpdateStoredValue(value); }
    }

    public struct OpaqueRef { }
}
