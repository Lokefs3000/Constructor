using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Inspector.V2.Values
{
    public class InspectorValue<T> : IInspectorValue
    {
        protected readonly IInspectorObject? _parentObject;
        protected readonly InspectorValueSource _source;

        protected T? _value;

        internal InspectorValue()
        {
            _parentObject = null;
            _source = default;

            _value = default;
        }

        public void UpdateValueFromValueType(ref OpaqueRef valueType)
        {

        }

        public void UpdateValueFromObject(object obj)
        {

        }
        public bool Equals(IInspectorValue other)
        {
            if (other is InspectorValue<T> value)
            {
                if (_value is IEquatable<T> equatable)
                    return equatable.Equals(value.Value);
                else
                    return _value == null ? value._value == null : _value.Equals(value._value);
            }

            return false;
        }

        private void UpdateStoredValue(T? value)
        {

        }


        public T? Value { get => _value; set => UpdateStoredValue(value); }
    }

    public struct OpaqueRef { }
}
