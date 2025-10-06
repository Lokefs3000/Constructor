using Primary.Input.Devices;

namespace Primary.Input.Bindings
{
    public sealed class Binding : IInputBinding
    {
        private string _name;
        private string _bindingPath;

        private IInputDevice? _device;
        private int _deviceValueId;

        private DeviceValue _value;

        internal Binding()
        {
            _name = string.Empty;
            _bindingPath = string.Empty;

            _device = null;
            _deviceValueId = IInputDevice.InvalidId;

            _value = default;
        }

        public bool UpdateValue()
        {
            DeviceValue newValue = _device == null ? default : _value = _device.ReadValue(_deviceValueId);
            _value = newValue;

            return _value.ValueBoolean;
        }

        public IInputBinding? FindBinding(ReadOnlySpan<char> name) => null;

        public string Name { get => _name; set => _name = value; }
        public DeviceValue StoredValue => _value;

        public string Path
        {
            get => _bindingPath; set
            {
                _bindingPath = value;
                InputSystem.Instance.ChangeDeviceForBinding(value, out _device, out _deviceValueId);
            }
        }

        public IInputDevice? Device => _device;
        public int DeviceValueId => _deviceValueId;
    }
}
