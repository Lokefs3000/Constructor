using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.State
{
    internal struct DirtyArray<T>
    {
        private T?[] _previous;
        private T[] _value;

        private byte _changed;

        public DirtyArray(int count, T value)
        {
            _previous = new T[count];
            _value = new T[count];

            _changed = 0;

            Array.Fill(_previous, default);
            Array.Fill(_value, default);
        }

        public void Fill(T value)
        {
            Array.Fill(_previous, value);
            Array.Fill(_value, value);

            _changed = 0xff;
        }

        public void ClearDirty()
        {
            Array.Copy(_value, _previous, _value.Length);
            _changed = 0;
        }

        public bool IsDirty(int index)
        {
            if (typeof(T).IsValueType)
            {
                return _previous[index]!.Equals(_value[index]);
            }
            else
            {
                return _previous[index]?.Equals(_value[index]) ?? _value[index] == null;
            }
        }

        public ref T GetWithoutDirty(int index)
        {
            Debug.Assert(index >= 0 && index < _value.Length);
            return ref _value.DangerousGetReferenceAt(index);
        }

        public ref T this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < _value.Length);

                _changed |= (byte)(1 << index);
                return ref _value[index];
            }
        }

        public bool IsAnyDirty { get => _changed > 0; set => _changed = (byte)(value ? 0xff : 0); }
        public int DirtyCount => 8 - BitOperations.LeadingZeroCount(_changed);

        public static implicit operator T[](DirtyArray<T> value) => value._value;
    }
}
