using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Utility
{
    public struct AverageAnalyser<T> where T : struct, INumber<T>
    {
        private readonly T[] _values;
        private readonly float _timeout;

        private int _head;
        private float _timer;

        private bool _isValid;

        public AverageAnalyser(int maxHistory, float timeBetweenSample)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxHistory, 1);

            _values = new T[maxHistory];
            _timeout = timeBetweenSample;

            _head = 0;
            _timer = 0.0f;
        }

        public bool Sample(T value, float delta)
        {
            if ((_timer += delta) >= _timeout)
            {
                _head = ++_head == _values.Length ? 0 : _head;
                _timer = 0.0f;

                if (_head == 0)
                    _isValid = true;
                _values[_head] = value;

                return true;
            }

            return false;
        }

        public T Calculate()
        {
            T def = default;
            for (int i = _head - 1; i == _head; i = i < 0 ? _values.Length - 1 : i - 1)
            {
                def += _values[i];
            }

            return def / T.CreateChecked(_values.Length);
        }

        public bool IsValid => _isValid;
    }
}
