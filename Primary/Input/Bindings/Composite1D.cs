using Primary.Input.Devices;

namespace Primary.Input.Bindings
{
    public sealed class Composite1D : IInputBinding
    {
        private string _name;

        private CompositeFavoritsm _favoritsm;

        private float _value;

        private List<Binding> _positiveBindings;
        private List<Binding> _negativeBindings;

        internal Composite1D()
        {
            _name = string.Empty;

            _favoritsm = CompositeFavoritsm.None;

            _value = default;

            _positiveBindings = new List<Binding>();
            _negativeBindings = new List<Binding>();
        }

        public bool UpdateValue()
        {
            bool positiveValue = false;
            bool negativeValue = false;

            for (int i = 0; i < _positiveBindings.Count; i++)
            {
                _positiveBindings[i].UpdateValue();
                if (_positiveBindings[i].StoredValue.ValueBoolean)
                {
                    positiveValue = true;
                    break;
                }
            }

            for (int i = 0; i < _negativeBindings.Count; i++)
            {
                _negativeBindings[i].UpdateValue();
                if (_negativeBindings[i].StoredValue.ValueBoolean)
                {
                    negativeValue = true;
                    break;
                }
            }

            if (positiveValue == negativeValue)
            {
                if (!positiveValue)
                    _value = 0.0f;
                else
                {
                    switch (_favoritsm)
                    {
                        case CompositeFavoritsm.None:
                        default: _value = 0.0f; break;
                        case CompositeFavoritsm.Positive: _value = 1.0f; break;
                        case CompositeFavoritsm.Negative: _value = -1.0f; break;
                    }
                }
            }
            else
            {
                if (positiveValue)
                    _value = 1.0f;
                else
                    _value = -1.0f;
            }

            return positiveValue || negativeValue;
        }

        public IInputBinding? FindBinding(ReadOnlySpan<char> name)
        {
            for (int i = 0; i < _positiveBindings.Count; i++)
            {
                if (name.Equals(_positiveBindings[i].Name, StringComparison.Ordinal))
                    return _positiveBindings[i];
            }

            for (int i = 0; i < _negativeBindings.Count; i++)
            {
                if (name.Equals(_negativeBindings[i].Name, StringComparison.Ordinal))
                    return _negativeBindings[i];
            }

            return null;
        }

        public Binding AddPositiveBinding()
        {
            Binding binding = new Binding();
            _positiveBindings.Add(binding);
            return binding;
        }

        public Binding AddNegativeBinding()
        {
            Binding binding = new Binding();
            _negativeBindings.Add(binding);
            return binding;
        }

        public void RemovePositiveBinding(Binding binding) => _positiveBindings.Remove(binding);
        public void RemoveNegativeBinding(Binding binding) => _negativeBindings.Remove(binding);

        public string Name { get => _name; set => _name = value; }
        public DeviceValue StoredValue => new DeviceValue(_value);

        public CompositeFavoritsm Favoritsm { get => _favoritsm; set => _favoritsm = value; }

        public IReadOnlyList<Binding> Positive => _positiveBindings;
        public IReadOnlyList<Binding> Negative => _negativeBindings;
    }
}
