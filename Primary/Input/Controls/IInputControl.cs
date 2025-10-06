using Primary.Input.Bindings;
using Primary.Input.Devices;

namespace Primary.Input.Controls
{
    public interface IInputControl
    {
        public DeviceValue Evaluate(IReadOnlyList<IInputBinding> bindings);
    }
}
