using Primary.Common;
using SDL;
using System.Numerics;

namespace Primary.Input.Devices
{
    public class PointerDevice : IInputDevice
    {
        private ButtonState[] _states;

        private Vector2 _mousePosition;
        private Vector2 _wheelDelta;
        private Vector2 _mouseDelta;

        internal PointerDevice()
        {
            _states = new ButtonState[3];
        }

        public bool HandleInputEvent(ref readonly SDL_Event @event)
        {
            if (@event.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN)
            {
                MouseButton button = TranslateButton(@event.button.Button);
                if (button != MouseButton.Unknown)
                {
                    if (!FlagUtility.HasFlag(_states[(int)button - 1], ButtonState.Held))
                    {
                        _states[(int)button - 1] |= ButtonState.Held | ButtonState.Updated;
                        return true;
                    }
                }
            }
            else if (@event.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP)
            {
                MouseButton button = TranslateButton(@event.button.Button);
                if (button != MouseButton.Unknown)
                {
                    if (FlagUtility.HasFlag(_states[(int)button - 1], ButtonState.Held))
                    {
                        _states[(int)button - 1] = ButtonState.Updated;
                        return true;
                    }
                }
            }
            else if (@event.Type == SDL_EventType.SDL_EVENT_MOUSE_MOTION)
            {
                _mousePosition = new Vector2(@event.motion.x, @event.motion.y);
                _mouseDelta = new Vector2(@event.motion.xrel, @event.motion.yrel);

                return true;
            }
            else if (@event.Type == SDL_EventType.SDL_EVENT_MOUSE_WHEEL)
            {
                _wheelDelta = new Vector2(@event.wheel.x, @event.wheel.y);

                return true;
            }

            return false;
        }

        public void UpdateFrame()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i] = FlagUtility.RemoveFlags(_states[i], ButtonState.Updated);
            }

            _mouseDelta = Vector2.Zero;
            _wheelDelta = Vector2.Zero;
        }

        public int ResolveBindingPath(ReadOnlySpan<char> bindingPath)
        {
            if (bindingPath == "Left")
                return new ValueCode(CodeType.Button, MouseButton.Left).Value;
            else if (bindingPath == "Middle")
                return new ValueCode(CodeType.Button, MouseButton.Middle).Value;
            else if (bindingPath == "Right")
                return new ValueCode(CodeType.Button, MouseButton.Right).Value;
            else if (bindingPath == "Motion")
                return new ValueCode(CodeType.Motion).Value;
            else if (bindingPath == "Wheel")
                return new ValueCode(CodeType.Wheel).Value;

            return IInputDevice.InvalidId;
        }

        public DeviceValue ReadValue(int valueId)
        {
            if (valueId == IInputDevice.InvalidId)
                return default;

            ValueCode code = new ValueCode(valueId);
            switch (code.Type)
            {
                case CodeType.Button: return new DeviceValue(FlagUtility.HasFlag(_states[(int)code.Button - 1], ButtonState.Held));
                case CodeType.Motion: return new DeviceValue(_mouseDelta);
                case CodeType.Wheel: return new DeviceValue(_wheelDelta);
                default: return default;
            }
        }

        public bool IsButtonHeld(MouseButton button) => FlagUtility.HasFlag(_states[(int)button - 1], ButtonState.Held);
        public bool IsButtonPressed(MouseButton button) => FlagUtility.HasFlag(_states[(int)button - 1], ButtonState.Held | ButtonState.Updated);
        public bool IsButtonReleased(MouseButton button) => FlagUtility.HasFlag(_states[(int)button - 1], ButtonState.Updated);

        public Vector2 MousePosition => _mousePosition;
        public Vector2 MouseDelta => _mouseDelta;
        public Vector2 WheelDelta => _wheelDelta;

        private static MouseButton TranslateButton(SDLButton button) => button switch
        {
            SDLButton.SDL_BUTTON_LEFT => MouseButton.Left,
            SDLButton.SDL_BUTTON_MIDDLE => MouseButton.Middle,
            SDLButton.SDL_BUTTON_RIGHT => MouseButton.Right,
            _ => MouseButton.Unknown
        };

        private enum ButtonState : byte
        {
            None = 0,

            Held = 1 << 0,
            Updated = 1 << 1,
        }

        private readonly record struct ValueCode
        {
            private readonly int _value;

            public ValueCode(int value) => _value = value;
            public ValueCode(CodeType type, MouseButton button) => _value = (int)type << 16 | (int)button;
            public ValueCode(CodeType type) => _value = (int)type << 16;

            public int Value => _value;

            public CodeType Type => (CodeType)((_value >> 16) & 0xff);
            public MouseButton Button => (MouseButton)(_value & 0xff);
        }

        private enum CodeType : byte
        {
            Unknown = 0,

            Button,
            Motion,
            Wheel
        }
    }

    public enum MouseButton : byte
    {
        Unknown = 0,

        Left,
        Middle,
        Right
    }
}
