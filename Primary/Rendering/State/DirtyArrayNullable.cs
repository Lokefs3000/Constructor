using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.State
{
    internal struct DirtyArrayNullable<T> where T : struct, IEquatable<T>
    {
        private readonly T?[] _previous;
        private readonly T?[] _value;

        private int _currentMin;
        private int _currentMax;

        private byte _changed;

        public DirtyArrayNullable(int count, T? value)
        {
            _previous = new T?[count];
            _value = new T?[count];

            _changed = 0;

            Array.Fill(_previous, value);
            Array.Fill(_value, value);

            if (value.HasValue)
            {
                _currentMin = 0;
                _currentMax = _value.Length;
            }
            else
            {
                _currentMin = 0;
                _currentMax = 0;
            }
        }

        public void Fill(T? value, bool makeInternalDataDirty)
        {
            Array.Fill(_previous, value);
            Array.Fill(_value, value);

            _changed = (byte)(makeInternalDataDirty ? 0xff : 0);

            if (value.HasValue)
            {
                _currentMin = 0;
                _currentMax = _value.Length;
            }
            else
            {
                _currentMin = 0;
                _currentMax = 0;
            }
        }

        public void FillNew(T? value)
        {
            Array.Fill(_value, value);
            _changed = 0xff;

            if (value.HasValue)
            {
                _currentMin = 0;
                _currentMax = _value.Length;
            }
            else
            {
                _currentMin = 0;
                _currentMax = 0;
            }
        }

        public void ClearDirty()
        {
            Array.Copy(_value, _previous, _value.Length);
            _changed = 0;
        }

        public readonly bool IsDirty(int index)
        {
            Guard.IsInRange(index, 0, _value.Length);

            ref T? previousVal = ref _previous.DangerousGetReferenceAt(index);
            ref T? currentVal = ref _value.DangerousGetReferenceAt(index);

            return !(currentVal.HasValue ?
                (previousVal.HasValue && currentVal.DangerousGetValueOrDefaultReference().Equals(previousVal.DangerousGetValueOrDefaultReference())) :
                previousVal.HasValue);
        }

        public T? this[int index]
        {
            get
            {
                Guard.IsInRange(index, 0, _value.Length);
                return _value.DangerousGetReferenceAt(index);
            }
            set
            {
                Guard.IsInRange(index, 0, _value.Length);

                _changed |= (byte)(1 << index);

                ref T? currentVal = ref _value.DangerousGetReferenceAt(index);
                currentVal = value;
            }
        }

        public bool IsAnyDirty { readonly get => _changed > 0; set => _changed = (byte)(value ? 0xff : 0); }
        public readonly int DirtyCount => 32 - BitOperations.LeadingZeroCount(_changed);
    }
}
