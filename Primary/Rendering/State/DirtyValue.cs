using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.State
{
    internal struct DirtyValue<T>  where T : IEquatable<T>
    {
        private T? _previous;
        private T _value;

        public DirtyValue(T value)
        {
            _previous = default;
            _value = value;
        }

        public T Value { get => _value; set => _value = value; }
        public bool IsDirty
        {
            get
            {
                if (typeof(T).IsValueType)
                {
                    return _previous!.Equals(_value);
                }
                else
                {
                    return _previous?.Equals(_value) ?? _value == null;
                }
            }
            set => _previous = _value;
        }

        public static implicit operator T(DirtyValue<T> value) => value._value;
    }
}
