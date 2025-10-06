using Primary.Input.Bindings;
using Primary.Input.Devices;

namespace Primary.Input.Controls
{
    public sealed class Vector2Control : IInputControl
    {
        public DeviceValue Evaluate(IReadOnlyList<IInputBinding> bindings)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].UpdateValue())
                {
                    return new DeviceValue(bindings[i].StoredValue.ValueVector2);
                }
            }

            return default;
        }
    }
}
