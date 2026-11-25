using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.State
{
    internal struct DirtyValueNullable<T> where T : struct, IEquatable<T>
    {
        private T? _previous;
        private T? _value;

        public DirtyValueNullable(T? value)
        {
            _previous = value;
            _value = value;
        }

        public void Reset(T? value)
        {
            _previous = value;
            _value = value;
        }

        public T? Value { readonly get => _value; set => _value = value; }
        public bool IsDirty
        {
            readonly get => !(_previous.HasValue ? _value.HasValue && _value.Value.Equals(_previous.Value) : !_value.HasValue);
            set => _previous = _value;
        }

        public static implicit operator T?(DirtyValueNullable<T> value) => value._value;
    }
}
