using Primary.Input.Devices;

namespace Primary.Input.Bindings
{
    public interface IInputBinding
    {
        public string Name { get; set; }
        public DeviceValue StoredValue { get; }

        public bool UpdateValue();

        public IInputBinding? FindBinding(ReadOnlySpan<char> name);
    }
}
