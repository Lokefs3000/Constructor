using Primary.Input.Bindings;
using Primary.Input.Devices;

namespace Primary.Input.Controls
{
    public sealed class ButtonControl : IInputControl
    {
        public DeviceValue Evaluate(IReadOnlyList<IInputBinding> bindings)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].UpdateValue())
                {
                    return new DeviceValue(bindings[i].StoredValue.ValueBoolean);
                }
            }

            return default;
        }
    }
}
