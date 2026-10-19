using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.State
{
    internal struct DirtyClassValue<T>  where T : class
    {
        private T? _previous;
        private T? _value;

        public DirtyClassValue(T? value)
        {
            _previous = default;
            _value = value;
        }

        public void Clear()
        {
            _previous = default; 
            _value = default;
        }

        public T? Value { readonly get => _value; set => _value = value; }
        public bool IsDirty
        {
            readonly get => _previous == null ? _value != null : !_previous.Equals(_value);
            set => _previous = _value;
        }

        public static implicit operator T?(DirtyClassValue<T> value) => value._value;
    }
}
