using Primary.Input.Bindings;
using Primary.Input.Controls;
using Primary.Input.Devices;

namespace Primary.Input
{
    public sealed class InputAction
    {
        private string _name;

        private IInputControl? _control;
        private List<IInputBinding> _bindings;

        private DeviceValue _value;

        internal InputAction()
        {
            _name = string.Empty;

            _control = null;
            _bindings = new List<IInputBinding>();

            _value = default;
        }

        internal void UpdateValues() => _value = _control?.Evaluate(_bindings) ?? default;

        public T AddBinding<T>() where T : IInputBinding, new()
        {
            T binding = new T();
            _bindings.Add(binding);
            return binding;
        }

        public void RemoveBinding(IInputBinding binding) => _bindings.Remove(binding);

        public string Name { get => _name; set => _name = value; }

        public IInputControl? Control { get => _control; set => _control = value; }
        public IReadOnlyList<IInputBinding> Bindings => _bindings;

        public ref readonly DeviceValue Value => ref _value;
    }
}
