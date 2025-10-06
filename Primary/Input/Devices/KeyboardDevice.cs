using SDL;

namespace Primary.Input.Devices
{
    public sealed class KeyboardDevice : IInputDevice
    {
        private bool[] _keyStates;
        private Dictionary<KeyCode, KeyState> _newlyUpdatedStates;

        internal KeyboardDevice()
        {
            _keyStates = new bool[byte.MaxValue];
            _newlyUpdatedStates = new Dictionary<KeyCode, KeyState>();
        }

        public bool HandleInputEvent(ref readonly SDL_Event @event)
        {
            if (@event.Type == SDL_EventType.SDL_EVENT_KEY_DOWN)
            {
                KeyCode key = TranslateKey(@event.key.key);
                if (key != KeyCode.Unknown)
                {
                    if (!_keyStates[(int)key])
                        _newlyUpdatedStates[key] = KeyState.Released;

                    _keyStates[(int)key] = true;
                    return true;
                }
            }
            else if (@event.Type == SDL_EventType.SDL_EVENT_KEY_UP)
            {
                KeyCode key = TranslateKey(@event.key.key);
                if (key != KeyCode.Unknown)
                {
                    if (_keyStates[(int)key])
                        _newlyUpdatedStates[key] = KeyState.Released;

                    _keyStates[(int)key] = false;
                    return true;
                }
            }

            return false;
        }

        public void UpdateFrame()
        {
            _newlyUpdatedStates.Clear();
        }

        public int ResolveBindingPath(ReadOnlySpan<char> bindingPath)
        {
            if (Enum.TryParse(bindingPath, true, out KeyCode result))
            {
                return (int)result;
            }

            return IInputDevice.InvalidId;
        }

        public DeviceValue ReadValue(int valueId)
        {
            if ((uint)valueId >= _keyStates.Length)
                return new DeviceValue(false);
            return new DeviceValue(_keyStates[valueId]);
        }

        public bool IsKeyDown(KeyCode key) => _keyStates[(int)key];
        public bool IsKeyPressed(KeyCode key) => _newlyUpdatedStates.TryGetValue(key, out KeyState state) && state == KeyState.Pressed;
        public bool IsKeyReleased(KeyCode key) => _newlyUpdatedStates.TryGetValue(key, out KeyState state) && state == KeyState.Released;

        private static KeyCode TranslateKey(SDL_Keycode keycode) => keycode switch
        {
            SDL_Keycode.SDLK_A => KeyCode.A,
            SDL_Keycode.SDLK_B => KeyCode.B,
            SDL_Keycode.SDLK_C => KeyCode.C,
            SDL_Keycode.SDLK_D => KeyCode.D,
            SDL_Keycode.SDLK_E => KeyCode.E,
            SDL_Keycode.SDLK_F => KeyCode.F,
            SDL_Keycode.SDLK_G => KeyCode.G,
            SDL_Keycode.SDLK_H => KeyCode.H,
            SDL_Keycode.SDLK_I => KeyCode.I,
            SDL_Keycode.SDLK_J => KeyCode.J,
            SDL_Keycode.SDLK_K => KeyCode.K,
            SDL_Keycode.SDLK_L => KeyCode.L,
            SDL_Keycode.SDLK_M => KeyCode.M,
            SDL_Keycode.SDLK_N => KeyCode.N,
            SDL_Keycode.SDLK_O => KeyCode.O,
            SDL_Keycode.SDLK_P => KeyCode.P,
            SDL_Keycode.SDLK_Q => KeyCode.Q,
            SDL_Keycode.SDLK_R => KeyCode.R,
            SDL_Keycode.SDLK_S => KeyCode.S,
            SDL_Keycode.SDLK_T => KeyCode.T,
            SDL_Keycode.SDLK_U => KeyCode.U,
            SDL_Keycode.SDLK_V => KeyCode.V,
            SDL_Keycode.SDLK_W => KeyCode.W,
            SDL_Keycode.SDLK_X => KeyCode.X,
            SDL_Keycode.SDLK_Y => KeyCode.Y,
            SDL_Keycode.SDLK_Z => KeyCode.Z,
            SDL_Keycode.SDLK_0 => KeyCode.Zero,
            SDL_Keycode.SDLK_1 => KeyCode.One,
            SDL_Keycode.SDLK_2 => KeyCode.Two,
            SDL_Keycode.SDLK_3 => KeyCode.Three,
            SDL_Keycode.SDLK_4 => KeyCode.Four,
            SDL_Keycode.SDLK_5 => KeyCode.Five,
            SDL_Keycode.SDLK_6 => KeyCode.Six,
            SDL_Keycode.SDLK_7 => KeyCode.Seven,
            SDL_Keycode.SDLK_8 => KeyCode.Eight,
            SDL_Keycode.SDLK_9 => KeyCode.Nine,
            SDL_Keycode.SDLK_RETURN => KeyCode.Return,
            SDL_Keycode.SDLK_ESCAPE => KeyCode.Escape,
            SDL_Keycode.SDLK_BACKSPACE => KeyCode.Backspace,
            SDL_Keycode.SDLK_TAB => KeyCode.Tab,
            SDL_Keycode.SDLK_SPACE => KeyCode.Space,
            SDL_Keycode.SDLK_EXCLAIM => KeyCode.Exclaim,
            SDL_Keycode.SDLK_DBLAPOSTROPHE => KeyCode.DoubleAposthrophe,
            SDL_Keycode.SDLK_HASH => KeyCode.Hash,
            SDL_Keycode.SDLK_DOLLAR => KeyCode.Dollar,
            SDL_Keycode.SDLK_PERCENT => KeyCode.Percent,
            SDL_Keycode.SDLK_AMPERSAND => KeyCode.Ampersand,
            SDL_Keycode.SDLK_APOSTROPHE => KeyCode.Apostrophe,
            SDL_Keycode.SDLK_LEFTPAREN => KeyCode.LeftParen,
            SDL_Keycode.SDLK_RIGHTPAREN => KeyCode.RightParen,
            SDL_Keycode.SDLK_ASTERISK => KeyCode.Asterisk,
            SDL_Keycode.SDLK_PLUS => KeyCode.Plus,
            SDL_Keycode.SDLK_COMMA => KeyCode.Comma,
            SDL_Keycode.SDLK_MINUS => KeyCode.Minus,
            SDL_Keycode.SDLK_PERIOD => KeyCode.Period,
            SDL_Keycode.SDLK_SLASH => KeyCode.Slash,
            SDL_Keycode.SDLK_COLON => KeyCode.Colon,
            SDL_Keycode.SDLK_SEMICOLON => KeyCode.Semicolon,
            SDL_Keycode.SDLK_LESS => KeyCode.Less,
            SDL_Keycode.SDLK_EQUALS => KeyCode.Equals,
            SDL_Keycode.SDLK_GREATER => KeyCode.Greater,
            SDL_Keycode.SDLK_QUESTION => KeyCode.Question,
            SDL_Keycode.SDLK_AT => KeyCode.At,
            SDL_Keycode.SDLK_LEFTBRACKET => KeyCode.LeftBracket,
            SDL_Keycode.SDLK_BACKSLASH => KeyCode.Backslash,
            SDL_Keycode.SDLK_RIGHTBRACKET => KeyCode.Rightbracket,
            SDL_Keycode.SDLK_CARET => KeyCode.Caret,
            SDL_Keycode.SDLK_UNDERSCORE => KeyCode.Underscore,
            SDL_Keycode.SDLK_GRAVE => KeyCode.Grave,
            SDL_Keycode.SDLK_LEFTBRACE => KeyCode.LeftBrace,
            SDL_Keycode.SDLK_PIPE => KeyCode.Pipe,
            SDL_Keycode.SDLK_RIGHTBRACE => KeyCode.RightBrace,
            SDL_Keycode.SDLK_TILDE => KeyCode.Tilde,
            SDL_Keycode.SDLK_DELETE => KeyCode.Delete,
            SDL_Keycode.SDLK_PLUSMINUS => KeyCode.PlusMinus,
            SDL_Keycode.SDLK_CAPSLOCK => KeyCode.CapsLock,
            SDL_Keycode.SDLK_F1 => KeyCode.F1,
            SDL_Keycode.SDLK_F2 => KeyCode.F2,
            SDL_Keycode.SDLK_F3 => KeyCode.F3,
            SDL_Keycode.SDLK_F4 => KeyCode.F4,
            SDL_Keycode.SDLK_F5 => KeyCode.F5,
            SDL_Keycode.SDLK_F6 => KeyCode.F6,
            SDL_Keycode.SDLK_F7 => KeyCode.F7,
            SDL_Keycode.SDLK_F8 => KeyCode.F8,
            SDL_Keycode.SDLK_F9 => KeyCode.F9,
            SDL_Keycode.SDLK_F10 => KeyCode.F10,
            SDL_Keycode.SDLK_F11 => KeyCode.F11,
            SDL_Keycode.SDLK_F12 => KeyCode.F12,
            SDL_Keycode.SDLK_F13 => KeyCode.F13,
            SDL_Keycode.SDLK_F14 => KeyCode.F14,
            SDL_Keycode.SDLK_F15 => KeyCode.F15,
            SDL_Keycode.SDLK_F16 => KeyCode.F16,
            SDL_Keycode.SDLK_F17 => KeyCode.F17,
            SDL_Keycode.SDLK_F18 => KeyCode.F18,
            SDL_Keycode.SDLK_F19 => KeyCode.F19,
            SDL_Keycode.SDLK_F20 => KeyCode.F20,
            SDL_Keycode.SDLK_F21 => KeyCode.F21,
            SDL_Keycode.SDLK_F22 => KeyCode.F22,
            SDL_Keycode.SDLK_F23 => KeyCode.F23,
            SDL_Keycode.SDLK_F24 => KeyCode.F24,
            SDL_Keycode.SDLK_PRINTSCREEN => KeyCode.PrintScreen,
            SDL_Keycode.SDLK_SCROLLLOCK => KeyCode.ScrollLock,
            SDL_Keycode.SDLK_PAUSE => KeyCode.Pause,
            SDL_Keycode.SDLK_INSERT => KeyCode.Insert,
            SDL_Keycode.SDLK_HOME => KeyCode.Home,
            SDL_Keycode.SDLK_PAGEUP => KeyCode.PageUp,
            SDL_Keycode.SDLK_END => KeyCode.End,
            SDL_Keycode.SDLK_PAGEDOWN => KeyCode.pageDown,
            SDL_Keycode.SDLK_RIGHT => KeyCode.Right,
            SDL_Keycode.SDLK_LEFT => KeyCode.Left,
            SDL_Keycode.SDLK_DOWN => KeyCode.Down,
            SDL_Keycode.SDLK_UP => KeyCode.Up,
            SDL_Keycode.SDLK_NUMLOCKCLEAR => KeyCode.NumLockClear,
            SDL_Keycode.SDLK_KP_DIVIDE => KeyCode.KpDivide,
            SDL_Keycode.SDLK_KP_MULTIPLY => KeyCode.KpMultiply,
            SDL_Keycode.SDLK_KP_MINUS => KeyCode.KpMinus,
            SDL_Keycode.SDLK_KP_PLUS => KeyCode.KpPlus,
            SDL_Keycode.SDLK_KP_ENTER => KeyCode.KpEnter,
            SDL_Keycode.SDLK_KP_PERIOD => KeyCode.KpPeriod,
            SDL_Keycode.SDLK_KP_EQUALS => KeyCode.KpEquals,
            SDL_Keycode.SDLK_KP_COMMA => KeyCode.KpComma,
            SDL_Keycode.SDLK_KP_0 => KeyCode.Kp0,
            SDL_Keycode.SDLK_KP_1 => KeyCode.Kp1,
            SDL_Keycode.SDLK_KP_2 => KeyCode.Kp2,
            SDL_Keycode.SDLK_KP_3 => KeyCode.Kp3,
            SDL_Keycode.SDLK_KP_4 => KeyCode.Kp4,
            SDL_Keycode.SDLK_KP_5 => KeyCode.Kp5,
            SDL_Keycode.SDLK_KP_6 => KeyCode.Kp6,
            SDL_Keycode.SDLK_KP_7 => KeyCode.Kp7,
            SDL_Keycode.SDLK_KP_8 => KeyCode.Kp8,
            SDL_Keycode.SDLK_KP_9 => KeyCode.Kp9,
            SDL_Keycode.SDLK_LCTRL => KeyCode.LeftControl,
            SDL_Keycode.SDLK_LSHIFT => KeyCode.LeftShift,
            SDL_Keycode.SDLK_LALT => KeyCode.LeftAlt,
            SDL_Keycode.SDLK_LGUI => KeyCode.LeftGui,
            SDL_Keycode.SDLK_RCTRL => KeyCode.RightControl,
            SDL_Keycode.SDLK_RSHIFT => KeyCode.RightShift,
            SDL_Keycode.SDLK_RALT => KeyCode.RightAlt,
            SDL_Keycode.SDLK_RGUI => KeyCode.RightGui,
            _ => KeyCode.Unknown
        };

        private enum KeyState : byte
        {
            Pressed = 0,
            Released
        }
    }

    public enum KeyCode : byte
    {
        Unknown = 0,

        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,

        Zero,
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,

        Return,
        Escape,
        Backspace,
        Tab,
        Space,
        Exclaim,
        DoubleAposthrophe,
        Hash,
        Dollar,
        Percent,
        Ampersand,
        Apostrophe,
        LeftParen,
        RightParen,
        Asterisk,
        Plus,
        Comma,
        Minus,
        Period,
        Slash,

        Colon,
        Semicolon,
        Less,
        Equals,
        Greater,
        Question,
        At,
        LeftBracket,
        Backslash,
        Rightbracket,
        Caret,
        Underscore,
        Grave,

        LeftBrace,
        Pipe,
        RightBrace,
        Tilde,
        Delete,
        PlusMinus,
        CapsLock,

        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,
        F13,
        F14,
        F15,
        F16,
        F17,
        F18,
        F19,
        F20,
        F21,
        F22,
        F23,
        F24,

        PrintScreen,
        ScrollLock,
        Pause,
        Insert,
        Home,
        PageUp,
        End,
        pageDown,
        Right,
        Left,
        Down,
        Up,
        NumLockClear,
        KpDivide,
        KpMultiply,
        KpMinus,
        KpPlus,
        KpEnter,
        KpPeriod,
        KpEquals,
        KpComma,

        Kp0,
        Kp1,
        Kp2,
        Kp3,
        Kp4,
        Kp5,
        Kp6,
        Kp7,
        Kp8,
        Kp9,

        LeftControl,
        LeftShift,
        LeftAlt,
        LeftGui,
        RightControl,
        RightShift,
        RightAlt,
        RightGui,
    }

    public enum KeyModifier : byte
    {
        None = 0,

        LeftControl = 1 << 0,
        LeftShift = 1 << 1,
        LeftAlt = 1 << 2,
        LeftGui = 1 << 3,

        RightControl = 1 << 4,
        RightShift = 1 << 5,
        RightAlt = 1 << 6,
        RightGui = 1 << 7,
    }
}
