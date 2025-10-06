using Primary.Input.Bindings;
using Primary.Input.Devices;

namespace Primary.Input.Controls
{
    public sealed class Vector3Control : IInputControl
    {
        public DeviceValue Evaluate(IReadOnlyList<IInputBinding> bindings)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].UpdateValue())
                {
                    return new DeviceValue(bindings[i].StoredValue.ValueVector3);
                }
            }

            return default;
        }
    }
}
