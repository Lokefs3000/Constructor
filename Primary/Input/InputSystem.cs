using Primary.Common;
using Primary.Input.Devices;
using Primary.Polling;
using SDL;
using System.Runtime.CompilerServices;

namespace Primary.Input
{
    public sealed class InputSystem : IEventHandler
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private List<InputScheme> _schemes;
        private Dictionary<string, IInputDevice> _devices;

        private KeyboardDevice _keyboardDevice;
        private PointerDevice _pointerDevice;

        private bool _hasUpdatePending;

        private bool _disposedValue;

        internal InputSystem()
        {
            s_instance.Target = this;

            _schemes = new List<InputScheme>();
            _devices = new Dictionary<string, IInputDevice>
            {
                { "<Keyboard>", new KeyboardDevice() },
                { "<Pointer>", new PointerDevice() },
            };

            _keyboardDevice = (_devices["<Keyboard>"] as KeyboardDevice)!;
            _pointerDevice = (_devices["<Pointer>"] as PointerDevice)!;

            Engine.GlobalSingleton.EventManager.AddHandler(this);
        }

        public void Handle(ref readonly SDL_Event @event)
        {
            foreach (var kvp in _devices)
            {
                bool r = kvp.Value.HandleInputEvent(in @event);
                if (r)
                    _hasUpdatePending = true;
            }
        }

        public void UpdatePending()
        {
            if (_hasUpdatePending)
            {
                foreach (InputScheme scheme in _schemes)
                {
                    scheme.UpdateActions();
                }
            }

            foreach (var kvp in _devices)
            {
                kvp.Value.UpdateFrame();
            }
        }

        public void ChangeDeviceForBinding(ReadOnlySpan<char> path, out IInputDevice? device, out int index)
        {
            int idx = path.IndexOf('/');
            if (idx == -1)
            {
                device = null;
                index = IInputDevice.InvalidId;

                return;
            }

            string devicePath = path.Slice(0, idx).ToString();
            if (!_devices.TryGetValue(devicePath, out device))
            {
                index = IInputDevice.InvalidId;

                return;
            }

            index = device.ResolveBindingPath(path.Slice(idx + 1));
        }

        /// <summary>Not thread-safe</summary>
        public static void AddScheme(InputScheme scheme)
        {
            InputSystem system = Instance;
            if (!system._schemes.Contains(scheme))
                system._schemes.Add(scheme);
        }

        /// <summary>Not thread-safe</summary>
        public static void RemoveScheme(InputScheme scheme) => Instance._schemes.Remove(scheme);

        public static IReadOnlyList<InputScheme> Schemes => Instance._schemes;

        public static KeyboardDevice Keyboard => Instance._keyboardDevice;
        public static PointerDevice Pointer => Instance._pointerDevice;

        public static InputSystem Instance => NullableUtility.ThrowIfNull(Unsafe.As<InputSystem>(s_instance.Target));
    }
}
