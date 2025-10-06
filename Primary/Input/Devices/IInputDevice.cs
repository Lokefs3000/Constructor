using SDL;
using System.Numerics;

namespace Primary.Input.Devices
{
    public interface IInputDevice
    {
        public bool HandleInputEvent(ref readonly SDL_Event @event);
        public void UpdateFrame();

        public int ResolveBindingPath(ReadOnlySpan<char> bindingPath);
        public DeviceValue ReadValue(int valueId);

        public const int InvalidId = int.MinValue;
    }

    public readonly record struct DeviceValue
    {
        private readonly Vector3 _value;

        public DeviceValue(bool value) => _value = new Vector3(value ? 1.0f : 0.0f);
        public DeviceValue(float value) => _value = new Vector3(value);
        public DeviceValue(Vector2 value) => _value = new Vector3(value, 0.0f);
        public DeviceValue(Vector3 value) => _value = value;

        public bool ValueBoolean => _value.X > 0.5f;
        public float ValueSingle => _value.X;
        public Vector2 ValueVector2 => new Vector2(_value.X, _value.Y);
        public Vector3 ValueVector3 => _value;
    }
}
