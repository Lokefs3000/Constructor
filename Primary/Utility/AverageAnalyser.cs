using System.Numerics;

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

        public T Max()
        {
            T def = _values[0];
            for (int i = 1; i < _values.Length; i++)
            {
                def = T.Max(def, _values[i]);
            }

            return def;
        }

        public T Min()
        {
            T def = _values[0];
            for (int i = 1; i < _values.Length; i++)
            {
                def = T.Min(def, _values[i]);
            }

            return def;
        }

        public int Count => _values.Length;
        public float Timeout => _timeout;

        public bool IsValid => _isValid;
    }
}
